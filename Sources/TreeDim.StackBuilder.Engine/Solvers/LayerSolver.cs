﻿using System;
using System.Collections.Generic;

using log4net;
using Sharp3D.Math.Core;

using treeDiM.StackBuilder.Basics;

namespace treeDiM.StackBuilder.Engine
{
    public class LayerSolver : ILayerSolver
    {
        public List<Layer2D> BuildLayers(
            Vector3D dimBox, Vector2D dimContainer,
            double offsetZ, /* e.g. pallet height */
            ConstraintSetAbstract constraintSet, bool keepOnlyBest)
        {
            // instantiate list of layers
            var listLayers0 = new List<Layer2D>();

            // loop through all patterns
            foreach (LayerPatternBox pattern in LayerPatternBox.All)
            {
                // loop through all orientation
                HalfAxis.HAxis[] patternAxes = pattern.IsSymetric ? HalfAxis.Positives : HalfAxis.All;
                foreach (HalfAxis.HAxis axisOrtho in patternAxes)
                {
                    // is orientation allowed
                    if (!constraintSet.AllowOrientation(Layer2D.VerticalAxis(axisOrtho)))
                        continue;

                    // not swapped vs swapped pattern
                    for (int iSwapped = 0; iSwapped < 2; ++iSwapped)
                    {
                        try
                        {
                            // does swapping makes sense for this layer pattern ?
                            if (!pattern.CanBeSwapped && (iSwapped == 1))
                                continue;
                            // instantiate layer
                            var layer = new Layer2D(dimBox, dimContainer, pattern.Name, axisOrtho, iSwapped == 1)
                            {
                                ForcedSpace = constraintSet.MinimumSpace.Value
                            };
                            if (layer.NoLayers(constraintSet.OptMaxHeight.Value) < 1)
                                continue;
                            layer.PatternName = pattern.Name;
                            if (!pattern.GetLayerDimensionsChecked(layer, out double actualLength, out double actualWidth))
                                continue;
                            pattern.GenerateLayer(layer, actualLength, actualWidth);
                            if (0 == layer.Count)
                                continue;
                            listLayers0.Add(layer);
                        }
                        catch (Exception ex)
                        {
                            _log.ErrorFormat("Pattern: {0} Orient: {1} Swapped: {2} Message: {3}"
                                , pattern.Name
                                , axisOrtho.ToString()
                                , iSwapped == 1 ? "True" : "False"
                                , ex.Message);
                        }
                    }
                }
            }
            // keep only best layers
            if (keepOnlyBest)
            {
                // 1. get best count
                int bestCount = 0;
                foreach (Layer2D layer in listLayers0)
                    bestCount = Math.Max(layer.CountInHeight(constraintSet.OptMaxHeight.Value - offsetZ), bestCount);

                // 2. remove any layer that does not match the best count given its orientation
                var listLayers1 = new List<Layer2D>();
                foreach (Layer2D layer in listLayers0)
                {
                    if (layer.CountInHeight(constraintSet.OptMaxHeight.Value - offsetZ) >= bestCount)
                        listLayers1.Add(layer);
                }
                // 3. copy back in original list
                listLayers0.Clear();
                listLayers0.AddRange(listLayers1);
            }
            if (constraintSet.OptMaxHeight.Activated)
                listLayers0.Sort(new LayerComparerCount(constraintSet, offsetZ));

            return listLayers0;
        }

        public Layer2D BuildLayer(Vector3D dimBox, Vector2D dimContainer, LayerDescBox layerDesc, double minSpace)
        {
            LayerDescBox layerDescBox = layerDesc as LayerDescBox;
            // instantiate layer
            var layer = new Layer2D(dimBox, dimContainer, layerDescBox.PatternName, layerDescBox.AxisOrtho, layerDesc.Swapped)
            {
                ForcedSpace = minSpace
            };
            // get layer pattern
            LayerPatternBox pattern = LayerPatternBox.GetByName(layerDesc.PatternName);
            // dimensions
            if (!pattern.GetLayerDimensionsChecked(layer, out double actualLength, out double actualWidth))
                return null;
            pattern.GenerateLayer(
                layer
                , actualLength
                , actualWidth);
            return layer;
        }

        public ILayer2D BuildLayer(Packable packable, Vector2D dimContainer, LayerDesc layerDesc, double minSpace)
        {
            ILayer2D layer = null;
            if (packable.IsBrick)
            {
                // casts
                LayerDescBox layerDescBox = layerDesc as LayerDescBox;
                // layer instantiation
                layer = new Layer2D(packable.OuterDimensions, dimContainer, layerDesc.PatternName, layerDescBox.AxisOrtho, layerDesc.Swapped) { ForcedSpace = minSpace };
                // get layer pattern
                LayerPatternBox pattern = LayerPatternBox.GetByName(layerDesc.PatternName);
                // dimensions
                if (!pattern.GetLayerDimensionsChecked(layer as Layer2D, out double actualLength, out double actualWidth))
                    return null;
                pattern.GenerateLayer(
                    layer as Layer2D
                    , actualLength
                    , actualWidth);
                return layer;
            }
            else if (packable.IsCylinder)
            {
                // casts
                CylinderProperties cylProperties = packable as CylinderProperties;
                // layer instantiation
                layer = new Layer2DCyl(cylProperties.RadiusOuter, cylProperties.Height, dimContainer, layerDesc.Swapped);
                // get layer pattern
                LayerPatternCyl pattern = LayerPatternCyl.GetByName(layerDesc.PatternName);
                double actualLength = 0.0, actualWidth = 0.0;
                if (!pattern.GetLayerDimensions(layer as Layer2DCyl, out actualLength, out actualWidth))
                    return null;
                pattern.GenerateLayer(layer as Layer2DCyl, actualLength, actualWidth);
            }
            else
            {
                throw new EngineException(string.Format("Unexpected packable {0} (Type = {1})", packable.Name, packable.GetType().ToString()));
            }
            return layer;
        }

        public ILayer2D BuildLayer(Packable packable, Vector2D dimContainer, LayerDesc layerDesc, Vector2D actualDimensions, double minSpace)
        {
            ILayer2D layer = null;
            LayerPattern pattern = null;
            if (packable.IsBrick)
            {
                LayerDescBox layerDescBox = layerDesc as LayerDescBox;
                // instantiate layer
                layer = new Layer2D(packable.OuterDimensions, dimContainer, layerDescBox.PatternName, layerDescBox.AxisOrtho, layerDesc.Swapped)
                {
                    ForcedSpace = minSpace
                };
                // get layer pattern
                pattern = LayerPatternBox.GetByName(layerDesc.PatternName);
            }
            else if (packable.IsCylinder)
            {
                CylinderProperties cylProperties = packable as CylinderProperties;
                layer = new Layer2DCyl(cylProperties.RadiusOuter, cylProperties.Height, dimContainer, layerDesc.Swapped);
                // get layer pattern
                pattern = LayerPatternCyl.GetByName(layerDesc.PatternName);
            }
            else
            {
                throw new EngineException(string.Format("Unexpected packable {0} (Type = {1})", packable.Name, packable.GetType().ToString()));
            }

            pattern.GenerateLayer(
                layer
                , layer.Swapped ? actualDimensions.Y : actualDimensions.X
                , layer.Swapped ? actualDimensions.X : actualDimensions.Y
                );
            return layer;
        }

        public Layer2D BuildLayer(Vector3D dimBox, Vector2D dimContainer, LayerDescBox layerDesc, Vector2D actualDimensions, double minSpace)
        {
            // instantiate layer
            var layer = new Layer2D(dimBox, dimContainer, layerDesc.PatternName, layerDesc.AxisOrtho, layerDesc.Swapped)
            {
                ForcedSpace = minSpace
            };
            // get layer pattern
            LayerPatternBox pattern = LayerPatternBox.GetByName(layerDesc.PatternName);
            // build layer
            pattern.GenerateLayer(
                layer
                , layer.Swapped ? actualDimensions.Y : actualDimensions.X
                , layer.Swapped ? actualDimensions.X : actualDimensions.Y);
            return layer;
        }
        /// <summary>
        /// Used to compute load dimension
        /// </summary>
        public bool GetDimensions(List<LayerDesc> layers, Packable packable, Vector2D dimContainer, double minSpace, out Vector2D actualDimensions)
        {
            actualDimensions = new Vector2D();
            foreach (LayerDesc layerDesc in layers)
            {
                // dimensions
                double actualLength = 0.0, actualWidth = 0.0;

                if (packable.IsBrick)
                {
                    LayerDescBox layerDescBox = layerDesc as LayerDescBox;
                    // instantiate layer
                    var layer = new Layer2D(packable.OuterDimensions, dimContainer, layerDescBox.PatternName, layerDescBox.AxisOrtho, layerDesc.Swapped)
                    {
                        ForcedSpace = minSpace
                    };
                    // get layer pattern
                    LayerPatternBox pattern = LayerPatternBox.GetByName(layerDesc.PatternName);
                    // dimensions
                    if (!pattern.GetLayerDimensionsChecked(layer, out actualLength, out actualWidth))
                    {
                        _log.Error(string.Format("Failed to get layer dimension : {0}", pattern.Name));
                        break;
                    }
                }
                else if (packable.IsCylinder)
                {
                    CylinderProperties cylProp = packable as CylinderProperties;
                    // instantiate layer
                    var layer = new Layer2DCyl(cylProp.RadiusOuter, cylProp.Height, dimContainer, layerDesc.Swapped);
                    // get layer pattern
                    LayerPatternCyl pattern = LayerPatternCyl.GetByName(layerDesc.PatternName);
                    // dimensions
                    if (!pattern.GetLayerDimensions(layer, out actualLength, out actualWidth))
                    {
                        _log.Error(string.Format("Failed to get layer dimension : {0}", pattern.Name));
                        break;
                    }
                }
                else
                {
                    throw new EngineException(string.Format("Unexpected packable {0} (Type = {1})", packable.Name, packable.GetType().ToString()));
                }

                actualDimensions.X = Math.Max(actualDimensions.X, layerDesc.Swapped ? actualWidth : actualLength);
                actualDimensions.Y = Math.Max(actualDimensions.Y, layerDesc.Swapped ? actualLength : actualWidth);
            }
            return true;
        }

        public static bool GetBestCombination(Vector3D dimBox, Vector3D dimContainer
            , ConstraintSetAbstract constraintSet, ref List<KeyValuePair<LayerDesc, int>> listLayer)
        {
            var layDescs = new LayerDesc[3];
            var counts = new int[3] { 0, 0, 0 };
            var heights = new double[3] { 0.0, 0.0, 0.0 };

            // loop through all patterns
            foreach (LayerPatternBox pattern in LayerPatternBox.All)
            {
                // loop through all orientation
                HalfAxis.HAxis[] patternAxes = pattern.IsSymetric ? HalfAxis.Positives : HalfAxis.All;
                foreach (HalfAxis.HAxis axisOrtho in patternAxes)
                {
                    // is orientation allowed
                    if (!constraintSet.AllowOrientation(Layer2D.VerticalAxis(axisOrtho)))
                        continue;
                    // not swapped vs swapped pattern
                    for (int iSwapped = 0; iSwapped < 2; ++iSwapped)
                    {
                        // does swapping makes sense for this layer pattern ?
                        if (!pattern.CanBeSwapped && (iSwapped == 1))
                            continue;
                        // instantiate layer
                        Vector2D layerDim = new Vector2D(dimContainer.X, dimContainer.Y);
                        var layer = new Layer2D(dimBox, layerDim, pattern.Name, axisOrtho, iSwapped == 1);
                        if (!pattern.GetLayerDimensionsChecked(layer, out double actualLength, out double actualWidth))
                            continue;
                        pattern.GenerateLayer(layer, actualLength, actualWidth);
                        if (0 == layer.Count)
                            continue;

                        int iAxisIndex = HalfAxis.Direction(axisOrtho);
                        if (layer.Count > counts[iAxisIndex])
                        {
                            counts[iAxisIndex] = layer.Count;
                            layDescs[iAxisIndex] = layer.LayerDescriptor;
                            heights[iAxisIndex] = layer.BoxHeight;
                        }
                    }
                }
            }

            double stackingHeight = dimContainer.Z;

            // single layer
            int indexIMax = 0, indexJMax = 0, noIMax = 0, noJMax = 0, iCountMax = 0;
            for (int i=0; i<3; ++i)
            {
                int noLayers = 0;
                if (counts[i] > 0)
                    noLayers = (int)Math.Floor(stackingHeight / heights[i]);
                if (counts[i] * noLayers > iCountMax)
                {
                    iCountMax = counts[i] * noLayers;
                    indexIMax = i;
                    noIMax = noLayers;
                }
            }

            // layer combinations
            int[] comb1 = { 0, 1, 2 };
            int[] comb2 = { 1, 2, 0 };
            for (int i = 0; i < 3; ++i)
            {
                int iComb1 = comb1[i];
                int iComb2 = comb2[i];

                int noI = 0;
                if (counts[iComb1] != 0)
                    noI = (int)Math.Floor(stackingHeight / heights[iComb1]);
                // search all index
                while (noI > 0)
                {
                    double remainingHeight = stackingHeight - noI * heights[iComb1];
                    int noJ = 0;
                    if (counts[iComb2] != 0)
                        noJ = (int)Math.Floor(remainingHeight / heights[iComb2]);
                    if (noI * counts[iComb1] + noJ * counts[iComb2] > iCountMax)
                    {
                        indexIMax = iComb1;  indexJMax = iComb2;
                        noIMax = noI;   noJMax = noJ;
                        iCountMax = noI * counts[iComb1] + noJ * counts[iComb2];
                    }
                    --noI;
                } // while
            }
            if (noIMax > 0)
                listLayer.Add(new KeyValuePair<LayerDesc, int>(layDescs[indexIMax], noIMax));
            if (noJMax > 0)
                listLayer.Add(new KeyValuePair<LayerDesc, int>(layDescs[indexJMax], noJMax));
            return true;
        }

        public List<Layer2DCyl> BuildLayers(
            double radius, double height
            , Vector2D dimContainer
            , double offsetZ /* e.g. pallet height */
            , ConstraintSetAbstract constraintSet
            , bool keepOnlyBest)
        {
            var listLayers0 = new List<Layer2DCyl>();
            foreach (LayerPatternCyl pattern in LayerPatternCyl.All)
            {            
                // not swapped vs swapped pattern
                for (int iSwapped = 0; iSwapped < 2; ++iSwapped)
                {
                    // does swapping makes sense for this layer pattern ?
                    if (!pattern.CanBeSwapped && (iSwapped == 1))
                        continue;
                    // instantiate layer
                    var layer = new Layer2DCyl(radius, height, dimContainer, iSwapped == 1);
                    layer.PatternName = pattern.Name;

                    double actualLength = 0.0, actualWidth = 0.0;
                    if (!pattern.GetLayerDimensions(layer, out actualLength, out actualWidth))
                        continue;
                    pattern.GenerateLayer(layer, actualLength, actualWidth);
                    listLayers0.Add(layer);
                }

                // keep only best layers
                if (keepOnlyBest)
                {
                    // 1. get best count
                    int bestCount = 0;
                    foreach (Layer2DCyl layer in listLayers0)
                        bestCount = Math.Max(layer.CountInHeight(constraintSet.OptMaxHeight.Value - offsetZ), bestCount);

                    // 2. remove any layer that does not match the best count given its orientation
                    listLayers0.RemoveAll(layer => 
                        !(layer.CountInHeight(constraintSet.OptMaxHeight.Value - offsetZ) >= bestCount));
                }
                listLayers0.Sort(new LayerCylComparerCount(constraintSet.OptMaxHeight.Value - offsetZ));
            }
            return listLayers0;
        }

        #region Non-Public Members
        protected static readonly ILog _log = LogManager.GetLogger(typeof(LayerSolver));
        #endregion
    }
}
