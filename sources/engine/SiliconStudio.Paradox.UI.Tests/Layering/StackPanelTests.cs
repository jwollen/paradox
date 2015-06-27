﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.UI.Panels;

namespace SiliconStudio.Paradox.UI.Tests.Layering
{
    /// <summary>
    /// Class for unit tests on <see cref="StackPanel"/>
    /// </summary>
    class StackPanelTests : StackPanel
    {
        private Random rand;

        /// <summary>
        /// Initialize the series of tests.
        /// </summary>
        [TestFixtureSetUp]
        public void InitializeTest()
        {
            // create a rand variable changing from a test to the other
            rand = new Random(DateTime.Now.Millisecond);
        }

        public void TestAll()
        {
            InitializeTest();
            TestProperties();
            TestCollapseOverride();
            TestMeasureOverride(); 
            TestArrangeOverride();
        }

        private void ResetState()
        {
            Children.Clear();
            DependencyProperties = new PropertyContainer(this);
            InvalidateMeasure();
            InvalidateArrange();
        }

        /// <summary>
        /// Tests the stack panel properties
        /// </summary>
        [Test]
        public void TestProperties()
        {
            ResetState();

            // test default values
            Assert.AreEqual(Orientation.Vertical, DependencyProperties.Get(OrientationPropertyKey));
        }

        /// <summary>
        /// Test the stack panel <see cref="StackPanel.CollapseOverride"/> function.
        /// </summary>
        [Test]
        public void TestCollapseOverride()
        {
            ResetState();

            // create two children
            var childOne = new StackPanelTests();
            var childTwo = new StackPanelTests();

            // set fixed size to the children
            childOne.Width = 1;
            childOne.Height = 2;
            childOne.Depth = 3;
            childTwo.Width = 10;
            childTwo.Height = 20;
            childTwo.Depth = 30;

            // add the children to the stack panel 
            Children.Add(childOne);
            Children.Add(childTwo);

            // arrange the stack panel and check children size
            Arrange(1000 * rand.NextVector3(), true);
            Assert.AreEqual(Vector3.Zero, childOne.RenderSize);
            Assert.AreEqual(Vector3.Zero, childTwo.RenderSize);
        }
        
        /// <summary>
        /// Test <see cref="StackPanel.MeasureOverride"/>.
        /// </summary>
        [Test]
        public void TestMeasureOverride()
        {
            ResetState();

            // test that desired size is null if no children
            Measure(1000 * rand.NextVector3());
            Assert.AreEqual(Vector3.Zero, DesiredSize);

            // Create and add children
            var child1 = new MeasureValidator();
            var child2 = new MeasureValidator();
            var child3 = new MeasureValidator();
            Children.Add(child1);
            Children.Add(child2);
            Children.Add(child3);
            
            // tests desired size depending on the orientation
            TestMeasureOverrideCore(Orientation.Horizontal);
            TestMeasureOverrideCore(Orientation.Vertical);
            TestMeasureOverrideCore(Orientation.InDepth);
        }
        
        private void TestMeasureOverrideCore(Orientation orientation)
        {
            // set the stack orientation
            Orientation = orientation;

            // set children margins
            Children[0].Margin = rand.NextThickness(10, 11, 12, 13, 14, 15);
            Children[1].Margin = rand.NextThickness(10, 11, 12, 13, 14, 15);
            Children[2].Margin = rand.NextThickness(10, 11, 12, 13, 14, 15);
            
            // set an available size
            var availablesizeWithMargins = 1000 * rand.NextVector3();
            var availableSizeWithoutMargins = CalculateSizeWithoutThickness(ref availablesizeWithMargins, ref MarginInternal);
            
            // set the validator expected and return values
            foreach (MeasureValidator child in Children)
            {
                // set the children desired size via the Measure override return value
                child.ReturnedMeasuredValue = 100 * rand.NextVector3();

                // set the expected size for child provided size validation
                var expectedSize = CalculateSizeWithoutThickness(ref availableSizeWithoutMargins, ref child.MarginInternal);
                expectedSize[(int)Orientation] = float.PositiveInfinity;
                child.ExpectedMeasureValue = expectedSize;
            }

            // Measure the stack
            Measure(availablesizeWithMargins);

            // compute the children max desired sizes
            var maximumDesiredSizeWithMargins = Vector3.Zero;
            foreach (var child in Children)
            {
                maximumDesiredSizeWithMargins = new Vector3(
                    Math.Max(maximumDesiredSizeWithMargins.X, child.DesiredSizeWithMargins.X),
                    Math.Max(maximumDesiredSizeWithMargins.Y, child.DesiredSizeWithMargins.Y),
                    Math.Max(maximumDesiredSizeWithMargins.Z, child.DesiredSizeWithMargins.Z));
            }

            // compute the children accumulated sizes
            var acculumatedDesiredSizeWithMargins = Children.Aggregate(Vector3.Zero, (current, child) => current + child.DesiredSizeWithMargins);
            
            // Checks the desired size
            switch (orientation)
            {
                case Orientation.Horizontal:
                    Assert.AreEqual(acculumatedDesiredSizeWithMargins.X, DesiredSize.X);
                    Assert.AreEqual(maximumDesiredSizeWithMargins.Y, DesiredSize.Y);
                    Assert.AreEqual(maximumDesiredSizeWithMargins.Z, DesiredSize.Z);
                    break;
                case Orientation.Vertical:
                    Assert.AreEqual(maximumDesiredSizeWithMargins.X, DesiredSize.X);
                    Assert.AreEqual(acculumatedDesiredSizeWithMargins.Y, DesiredSize.Y);
                    Assert.AreEqual(maximumDesiredSizeWithMargins.Z, DesiredSize.Z);
                    break;
                case Orientation.InDepth:
                    Assert.AreEqual(maximumDesiredSizeWithMargins.X, DesiredSize.X);
                    Assert.AreEqual(maximumDesiredSizeWithMargins.Y, DesiredSize.Y);
                    Assert.AreEqual(acculumatedDesiredSizeWithMargins.Z, DesiredSize.Z);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("orientation");
            }
        }
        
        /// <summary>
        /// Test for <see cref="StackPanel.ArrangeOverride"/>
        /// </summary>
        [Test]
        public void TestArrangeOverride()
        {
            ResetState();

            DepthAlignment = DepthAlignment.Stretch;

            // test that arrange set render size to provided size when there is no children
            var providedSize = 1000 * rand.NextVector3();
            var providedSizeWithoutMargins = CalculateSizeWithoutThickness(ref providedSize, ref MarginInternal);
            Measure(providedSize);
            Arrange(providedSize, false);
            Assert.AreEqual(providedSizeWithoutMargins, RenderSize);

            // tests desired size depending on the orientation
            TestArrangeOverrideCore(Orientation.Horizontal);
            TestArrangeOverrideCore(Orientation.Vertical);
            TestArrangeOverrideCore(Orientation.InDepth);
        }
        
        private void TestArrangeOverrideCore(Orientation orientation)
        {
            ResetState();

            DepthAlignment = DepthAlignment.Stretch;

            // Create and add children
            var child1 = new ArrangeValidator { DepthAlignment = DepthAlignment.Stretch };
            var child2 = new ArrangeValidator { DepthAlignment = DepthAlignment.Stretch };
            var child3 = new ArrangeValidator { DepthAlignment = DepthAlignment.Stretch };
            Children.Add(child1);
            Children.Add(child2);
            Children.Add(child3);

            // set the stack orientation
            Orientation = orientation;

            // set children margins
            Children[0].Margin = rand.NextThickness(10, 11, 12, 13, 14, 15);
            Children[1].Margin = rand.NextThickness(10, 11, 12, 13, 14, 15);
            Children[2].Margin = rand.NextThickness(10, 11, 12, 13, 14, 15);

            // set an available size
            var availablesizeWithMargins = 1000 * rand.NextVector3();
            var availableSizeWithoutMargins = CalculateSizeWithoutThickness(ref availablesizeWithMargins, ref MarginInternal);

            // set the arrange validator values
            foreach (ArrangeValidator child in Children)
            {
                child.ReturnedMeasuredValue = 1000 * rand.NextVector3();
                child.ExpectedArrangeValue = CalculateSizeWithoutThickness(ref availableSizeWithoutMargins, ref child.MarginInternal);
                child.ExpectedArrangeValue[(int)Orientation] = child.ReturnedMeasuredValue[(int)Orientation];
            }

            // Measure the stack
            Measure(availableSizeWithoutMargins);
            Arrange(availablesizeWithMargins, false);
            
            // compute the children accumulated sizes
            var acculumatedDesiredSizeWithMarginsList = new List<Vector3>();
            for (int i = 0; i < Children.Count; i++)
            {
                var accumulatedVector = Vector3.Zero;
                for (int j = 0; j < i; j++)
                {
                    for(int dim = 0; dim<3; ++dim)
                        accumulatedVector[dim] += Children[j].RenderSize[dim] + Children[j].Margin[dim] + Children[j].Margin[dim + 3];
                }

                acculumatedDesiredSizeWithMarginsList.Add(accumulatedVector);
            }

            // checks the stack arranged size
            Assert.AreEqual(availableSizeWithoutMargins, RenderSize);
            
            // Checks the children arrange matrix
            for (int i = 0; i < Children.Count; i++)
            {
                var childOffsets = -RenderSize / 2;

                switch (orientation)
                {
                    case Orientation.Horizontal:
                        childOffsets.X += acculumatedDesiredSizeWithMarginsList[i].X;
                        break;
                    case Orientation.Vertical:
                        childOffsets.Y += acculumatedDesiredSizeWithMarginsList[i].Y;
                        break;
                    case Orientation.InDepth:
                        childOffsets.Z += acculumatedDesiredSizeWithMarginsList[i].Z;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("orientation");
                }

                Utilities.AssertAreNearlyEqual(Matrix.Translation(childOffsets), Children[i].DependencyProperties.Get(PanelArrangeMatrixPropertyKey));
            }
        }

        /// <summary>
        /// Test the invalidations generated object property changes.
        /// </summary>
        [Test]
        public void TestBasicInvalidations()
        {
            ResetState();

            // - test the properties that are supposed to invalidate the object measurement
            UIElementLayeringTests.TestMeasureInvalidation(this, () => Orientation = Orientation.InDepth);
        }

        /// <summary>
        /// Test for <see cref="StackPanel.CanScroll"/>
        /// </summary>
        [Test]
        public void TestCanScroll()
        {
            var stackPanel = new StackPanel();

            stackPanel.Orientation = Orientation.Horizontal;
            AssertCanScroll(stackPanel);
        }

        private void AssertCanScroll(StackPanel stackPanel)
        {
            for (int i = 0; i < 3; i++)
                Assert.AreEqual(i==(int)stackPanel.Orientation, stackPanel.CanScroll((Orientation)i));
        }

        /// <summary>
        /// Test for <see cref="StackPanel.Extent"/>
        /// </summary>
        [Test]
        public void TestExtent()
        {
            TestExtent(Orientation.Horizontal);
            TestExtent(Orientation.Vertical);
            TestExtent(Orientation.InDepth);
        }

        private void TestExtent(Orientation direction)
        {
            var stackSize = new Vector3(100, 200, 300);
            var childSize1 = new Vector3(50, 150, 250);
            var childSize2 = new Vector3(150, 250, 350);

            var stackPanel = new StackPanel { Size = stackSize, Orientation = direction };

            Assert.AreEqual(Vector3.Zero, stackPanel.Extent);

            var child1 = new StackPanel { Size = childSize1 };
            var child2 = new StackPanel { Size = childSize2 };
            var child3 = new StackPanel { Size = childSize1 };

            stackPanel.Children.Add(child1);
            stackPanel.Children.Add(child2);
            stackPanel.Children.Add(child3);

            stackPanel.Arrange(Vector3.Zero, false);

            var exactReferenceExtent = stackSize;
            exactReferenceExtent[(int)direction] = 0;
            foreach (var child in stackPanel.Children)
                exactReferenceExtent[(int)direction] += child.Size[(int)direction];

            Assert.AreEqual(exactReferenceExtent, stackPanel.Extent);

            // with virtualized items.
            stackPanel.ItemVirtualizationEnabled = true;

            stackPanel.Arrange(Vector3.Zero, false);

            var childCount = 0;
            var approximatedSize = 0f;
            var approximatedReferenceExtent = stackSize;
            while (childCount < stackPanel.Children.Count-1 && approximatedSize < stackPanel.Size[(int)direction])
            {
                ++childCount;
                approximatedSize += stackPanel.Children[stackPanel.Children.Count - childCount].Size[(int)direction];
            }
            approximatedReferenceExtent[(int)direction] = stackPanel.Children.Count / (float)childCount * approximatedSize;

            Assert.AreEqual(approximatedReferenceExtent, stackPanel.Extent);
        }

        /// <summary>
        /// Test for <see cref="StackPanel.Offset"/>
        /// </summary>
        [Test]
        public void TestOffset()
        {
            var stackSize = new Vector3(100, 200, 300);
            var childSize1 = new Vector3(50, 150, 250);
            var childSize2 = new Vector3(150, 250, 350);
            var childSize3 = new Vector3(250, 250, 350);
            var childSize4 = new Vector3(350, 250, 350);

            var stackPanel = new StackPanel { Size = stackSize, Orientation = Orientation.Horizontal };

            Assert.AreEqual(Vector3.Zero, stackPanel.Offset);

            var child1 = new StackPanel { Size = childSize1 };
            var child2 = new StackPanel { Size = childSize2 };
            var child3 = new StackPanel { Size = childSize3 };
            var child4 = new StackPanel { Size = childSize4 };

            stackPanel.Children.Add(child1);
            stackPanel.Children.Add(child2);
            stackPanel.Children.Add(child3);
            stackPanel.Children.Add(child4);

            var refenceOffset = Vector3.Zero;

            // non virtualized children
            stackPanel.ScrolllToElement(1);
            stackPanel.Arrange(Vector3.Zero, false);
            refenceOffset[0] -= childSize1.X;
            Assert.AreEqual(refenceOffset, stackPanel.Offset);

            stackPanel.ScrolllToElement(2.5f);
            stackPanel.Arrange(Vector3.Zero, false);
            refenceOffset[0] -= childSize2.X + childSize3.X / 2;
            Assert.AreEqual(refenceOffset, stackPanel.Offset);

            stackPanel.ScrollToEnd(Orientation.Horizontal);
            stackPanel.Arrange(Vector3.Zero, false);
            refenceOffset[0] = -childSize1.X - childSize2.X - childSize3.X - childSize4.X + stackPanel.Size.X;
            Assert.IsTrue((refenceOffset-stackPanel.Offset).Length() < 0.001);

            // virtualized children
            refenceOffset[0] = 0;
            stackPanel.ScrolllToElement(0);
            stackPanel.ItemVirtualizationEnabled = true;
            stackPanel.Arrange(Vector3.Zero, false);
            Assert.AreEqual(refenceOffset, stackPanel.Offset);

            refenceOffset[0] = 0;
            stackPanel.ScrolllToElement(1);
            stackPanel.Arrange(Vector3.Zero, false);
            Assert.AreEqual(refenceOffset, stackPanel.Offset);

            refenceOffset[0] = -childSize3.X / 2;
            stackPanel.ScrolllToElement(2.5f);
            stackPanel.Arrange(Vector3.Zero, false);
            Assert.AreEqual(refenceOffset, stackPanel.Offset);

            stackPanel.ScrollToEnd(Orientation.Horizontal);
            stackPanel.Arrange(Vector3.Zero, false);
            refenceOffset[0] = -childSize4.X + stackPanel.Size.X;
            Assert.IsTrue((refenceOffset-stackPanel.Offset).Length() < 0.001);
        }

        /// <summary>
        /// Test for <see cref="StackPanel.ScrollPosition"/>
        /// </summary>
        [Test]
        public void TestScrollPosition()
        {
            TestScrollPosition(false);
            TestScrollPosition(true);
        }

        private static void TestScrollPosition(bool virtualizeChildren)
        {
            var stackSize = new Vector3(100, 200, 300);
            var childSize1 = new Vector3(50, 150, 250);
            var childSize2 = new Vector3(150, 250, 350);
            var childSize3 = new Vector3(250, 250, 350);
            var childSize4 = new Vector3(350, 250, 350);

            var stackPanel = new StackPanel { Size = stackSize, Orientation = Orientation.Horizontal, ItemVirtualizationEnabled = virtualizeChildren};

            Assert.AreEqual(0f, stackPanel.ScrollPosition);

            var child1 = new StackPanel { Size = childSize1 };
            var child2 = new StackPanel { Size = childSize2 };
            var child3 = new StackPanel { Size = childSize3 };
            var child4 = new StackPanel { Size = childSize4 };

            stackPanel.Children.Add(child1);
            stackPanel.Children.Add(child2);
            stackPanel.Children.Add(child3);
            stackPanel.Children.Add(child4);

            float referencePosition = 0;
            stackPanel.ScrolllToElement(referencePosition);
            stackPanel.Arrange(Vector3.Zero, false);
            Assert.AreEqual(referencePosition, stackPanel.ScrollPosition);

            referencePosition = 1;
            stackPanel.ScrolllToElement(referencePosition);
            stackPanel.Arrange(Vector3.Zero, false);
            Assert.AreEqual(referencePosition, stackPanel.ScrollPosition);

            referencePosition = 2;
            stackPanel.ScrolllToElement(referencePosition);
            stackPanel.Arrange(Vector3.Zero, false);
            Assert.AreEqual(referencePosition, stackPanel.ScrollPosition);

            referencePosition = 2.3f;
            stackPanel.ScrolllToElement(referencePosition);
            stackPanel.Arrange(Vector3.Zero, false);
            Assert.AreEqual(referencePosition, stackPanel.ScrollPosition);

            stackPanel.ScrollToEnd(Orientation.Horizontal);
            referencePosition = 3 + (childSize4.X - stackSize.X) / childSize4.X;
            stackPanel.Arrange(Vector3.Zero, false);
            Assert.IsTrue(Math.Abs(referencePosition - stackPanel.ScrollPosition) < MathUtil.ZeroTolerance);
        }

        /// <summary>
        /// Test for <see cref="StackPanel.Viewport"/>
        /// </summary>
        [Test]
        public void TestViewport()
        {
            TestViewport(false);
            TestViewport(true);
        }

        public void TestViewport(bool virtualizeChildren)
        {
            var random = new Random();
            var childSize1 = new Vector3(50, 150, 250);
            var childSize2 = new Vector3(150, 250, 350);
            var childSize3 = new Vector3(250, 250, 350);
            var childSize4 = new Vector3(350, 250, 350);

            var stackPanel = new StackPanel { DepthAlignment = DepthAlignment.Stretch, ItemVirtualizationEnabled = virtualizeChildren };

            Assert.AreEqual(Vector3.Zero, stackPanel.Viewport);

            var child1 = new StackPanel { Size = childSize1 };
            var child2 = new StackPanel { Size = childSize2 };
            var child3 = new StackPanel { Size = childSize3 };
            var child4 = new StackPanel { Size = childSize4 };

            stackPanel.Children.Add(child1);
            stackPanel.Children.Add(child2);
            stackPanel.Children.Add(child3);
            stackPanel.Children.Add(child4);

            var referencePosition = Vector3.Zero;
            stackPanel.Arrange(referencePosition, false);
            Assert.AreEqual(referencePosition, stackPanel.Viewport);

            referencePosition = random.NextVector3();
            stackPanel.Arrange(referencePosition, false);
            Assert.AreEqual(referencePosition, stackPanel.Viewport);

            referencePosition = random.NextVector3();
            stackPanel.ScrollToEnd(Orientation.Horizontal);
            stackPanel.ScrollToEnd(Orientation.Vertical);
            stackPanel.Children.Remove(child4);
            stackPanel.Arrange(referencePosition, false);
            Assert.AreEqual(referencePosition, stackPanel.Viewport);

            var stackSize = new Vector3(100, 200, 300);
            stackPanel.Size = stackSize;
            stackPanel.Arrange(Vector3.Zero, false);
            Assert.AreEqual(stackSize, stackPanel.Viewport);
        }

        /// <summary>
        /// Test for <see cref="StackPanel.ScrollBarPositions"/>
        /// </summary>
        [Test]
        public void TestScrollBarPosition()
        {
            TestScrollBarPosition(false);
            TestScrollBarPosition(true);
        }

        private static void TestScrollBarPosition(bool virtualizeChildren)
        {
            var stackSize = new Vector3(100, 200, 300);
            var childSize1 = new Vector3(50, 150, 250);
            var childSize2 = new Vector3(150, 250, 350);
            var childSize3 = new Vector3(250, 250, 350);

            var stackPanel = new StackPanel { Size = stackSize, ItemVirtualizationEnabled = virtualizeChildren, Orientation = Orientation.Horizontal };

            Assert.AreEqual(Vector3.Zero, stackPanel.ScrollBarPositions);

            var child1 = new StackPanel { Size = childSize1 };
            var child2 = new StackPanel { Size = childSize2 };
            var child3 = new StackPanel { Size = childSize3 };

            stackPanel.Children.Add(child1);
            stackPanel.Children.Add(child2);
            stackPanel.Children.Add(child3);

            var reference = Vector3.Zero;
            stackPanel.ScrollToBeginning(Orientation.Horizontal);
            stackPanel.Arrange(Vector3.Zero, false);
            Assert.AreEqual(reference, stackPanel.ScrollBarPositions);
            
            reference[0] = 1;
            stackPanel.ScrollToEnd(Orientation.Horizontal);
            stackPanel.Arrange(Vector3.Zero, false);
            Assert.AreEqual(reference, stackPanel.ScrollBarPositions);


            stackPanel.ScrolllToElement(1);
            stackPanel.Arrange(Vector3.Zero, false);
            if (virtualizeChildren)
            {
                reference[0] = 1 / (2 + (childSize3.X - stackPanel.Size.X) / childSize3.X);
            }
            else
            {
                reference[0] = childSize1.X / (childSize1.X + childSize2.X + childSize3.X - stackPanel.Size.X);
            }
            Assert.AreEqual(reference, stackPanel.ScrollBarPositions);
        }

        /// <summary>
        /// Test for the <see cref="StackPanel.ScrollToPreviousLine"/> and <see cref="StackPanel.ScrollToNextLine"/>
        /// </summary>
        [Test]
        public void TestScrollToNeighborElement()
        {
            TestScrollToNeighborElement(false);
            TestScrollToNeighborElement(true);
        }

        public void TestScrollToNeighborElement(bool virtualizeItems)
        {
            var stackSize = new Vector3(100, 200, 300);
            var childSize1 = new Vector3(50, 150, 250);
            var childSize2 = new Vector3(150, 250, 350);
            var childSize3 = new Vector3(250, 250, 350);

            var stackPanel = new StackPanel { Size = stackSize, ItemVirtualizationEnabled = virtualizeItems, Orientation = Orientation.Horizontal };

            var child1 = new StackPanel { Size = childSize1 };
            var child2 = new StackPanel { Size = childSize2 };
            var child3 = new StackPanel { Size = childSize3 };

            stackPanel.Children.Add(child1);
            stackPanel.Children.Add(child2);
            stackPanel.Children.Add(child3);

            stackPanel.Arrange(Vector3.Zero, false);

            // pre-arranged
            stackPanel.ScrollToNextLine(Orientation.Horizontal);
            Assert.AreEqual(1, stackPanel.ScrollPosition);

            stackPanel.ScrollToPreviousLine(Orientation.Horizontal);
            Assert.AreEqual(0, stackPanel.ScrollPosition);

            stackPanel.ScrolllToElement(1.6f);
            stackPanel.ScrollToPreviousLine(Orientation.Horizontal);
            Assert.AreEqual(1, stackPanel.ScrollPosition);

            stackPanel.ScrolllToElement(1.6f);
            stackPanel.ScrollToNextLine(Orientation.Horizontal);
            Assert.AreEqual(2, stackPanel.ScrollPosition);

            // reset scrolling
            stackPanel.ScrollToBeginning(Orientation.Horizontal);
            stackPanel.Arrange(Vector3.Zero, false);

            // post arranged
            stackPanel.InvalidateArrange();
            stackPanel.ScrollToNextLine(Orientation.Horizontal);
            Assert.AreEqual(0, stackPanel.ScrollPosition);
            stackPanel.Arrange(Vector3.Zero, false);
            Assert.AreEqual(1, stackPanel.ScrollPosition);

            stackPanel.InvalidateArrange();
            stackPanel.ScrollToPreviousLine(Orientation.Horizontal);
            Assert.AreEqual(1, stackPanel.ScrollPosition);
            stackPanel.Arrange(Vector3.Zero, false);
            Assert.AreEqual(0, stackPanel.ScrollPosition);

            stackPanel.ScrolllToElement(1.6f);
            stackPanel.Arrange(Vector3.Zero, false);
            stackPanel.InvalidateArrange();
            stackPanel.ScrollToPreviousLine(Orientation.Horizontal);
            Assert.AreEqual(1.6f, stackPanel.ScrollPosition);
            stackPanel.Arrange(Vector3.Zero, false);
            Assert.AreEqual(1, stackPanel.ScrollPosition);

            stackPanel.ScrolllToElement(1.6f);
            stackPanel.Arrange(Vector3.Zero, false);
            stackPanel.InvalidateArrange();
            stackPanel.ScrollToNextLine(Orientation.Horizontal);
            Assert.AreEqual(1.6f, stackPanel.ScrollPosition);
            stackPanel.Arrange(Vector3.Zero, false);
            Assert.AreEqual(2, stackPanel.ScrollPosition);
        }

        /// <summary>
        /// Test for the <see cref="StackPanel.ScrollToNextPage"/> and <see cref="StackPanel.ScrollToPreviousPage"/>
        /// </summary>
        [Test]
        public void TestScrollToNeighborScreen()
        {
            TestScrollToNeighborScreen(false);
            TestScrollToNeighborScreen(true);
        }

        public void TestScrollToNeighborScreen(bool virtualizeItems)
        {
            var stackSize = new Vector3(100, 200, 300);
            var childSize1 = new Vector3(50, 150, 250);
            var childSize2 = new Vector3(150, 250, 350);
            var childSize3 = new Vector3(250, 250, 350);

            var stackPanel = new StackPanel { Size = stackSize, ItemVirtualizationEnabled = virtualizeItems, Orientation = Orientation.Horizontal };

            var child1 = new StackPanel { Size = childSize1 };
            var child2 = new StackPanel { Size = childSize2 };
            var child3 = new StackPanel { Size = childSize3 };

            stackPanel.Children.Add(child1);
            stackPanel.Children.Add(child2);
            stackPanel.Children.Add(child3);

            stackPanel.Arrange(Vector3.Zero, false);

            // pre-arranged
            stackPanel.ScrollToNextPage(Orientation.Horizontal);
            Utilities.AssertAreNearlyEqual(1 + 1 / 3f, stackPanel.ScrollPosition);

            stackPanel.ScrollToPreviousPage(Orientation.Horizontal);
            Utilities.AssertAreNearlyEqual(0, stackPanel.ScrollPosition);

            stackPanel.ScrolllToElement(1 + 2 / 3f);
            stackPanel.ScrollToPreviousPage(Orientation.Horizontal);
            Utilities.AssertAreNearlyEqual(1f, stackPanel.ScrollPosition);

            stackPanel.ScrolllToElement(1 + 2 / 3f);
            stackPanel.ScrollToNextPage(Orientation.Horizontal);
            Utilities.AssertAreNearlyEqual(2.2f, stackPanel.ScrollPosition);

            // reset scrolling
            stackPanel.ScrollToBeginning(Orientation.Horizontal);
            stackPanel.Arrange(Vector3.Zero, false);

            // post arranged
            stackPanel.InvalidateArrange();
            stackPanel.ScrollToNextPage(Orientation.Horizontal);
            Utilities.AssertAreNearlyEqual(0, stackPanel.ScrollPosition);
            stackPanel.Arrange(Vector3.Zero, false);
            Utilities.AssertAreNearlyEqual(1 + 1 / 3f, stackPanel.ScrollPosition);

            stackPanel.InvalidateArrange();
            stackPanel.ScrollToPreviousPage(Orientation.Horizontal);
            Utilities.AssertAreNearlyEqual(1 + 1 / 3f, stackPanel.ScrollPosition);
            stackPanel.Arrange(Vector3.Zero, false);
            Utilities.AssertAreNearlyEqual(0, stackPanel.ScrollPosition);

            stackPanel.ScrolllToElement(1 + 2 / 3f);
            stackPanel.Arrange(Vector3.Zero, false);
            stackPanel.InvalidateArrange();
            stackPanel.ScrollToPreviousPage(Orientation.Horizontal);
            Utilities.AssertAreNearlyEqual(1 + 2 / 3f, stackPanel.ScrollPosition);
            stackPanel.Arrange(Vector3.Zero, false);
            Utilities.AssertAreNearlyEqual(1, stackPanel.ScrollPosition);

            stackPanel.ScrolllToElement(1 + 2 / 3f);
            stackPanel.Arrange(Vector3.Zero, false);
            stackPanel.InvalidateArrange();
            stackPanel.ScrollToNextPage(Orientation.Horizontal);
            Utilities.AssertAreNearlyEqual(1 + 2 / 3f, stackPanel.ScrollPosition);
            stackPanel.Arrange(Vector3.Zero, false);
            Utilities.AssertAreNearlyEqual(2.2f, stackPanel.ScrollPosition);
        }

        /// <summary>
        /// Test for the <see cref="StackPanel.ScrollToBeginning"/> and <see cref="StackPanel.ScrollToEnd"/>
        /// </summary>
        [Test]
        public void TestScrollToExtrema()
        {
            TestScrollToExtrema(false);
            TestScrollToExtrema(true);
        }

        public void TestScrollToExtrema(bool virtualizeItems)
        {
            var stackSize = new Vector3(100, 200, 300);
            var childSize1 = new Vector3(50, 150, 250);
            var childSize2 = new Vector3(150, 250, 350);
            var childSize3 = new Vector3(250, 250, 350);

            var stackPanel = new StackPanel { Size = stackSize, ItemVirtualizationEnabled = virtualizeItems, Orientation = Orientation.Horizontal };

            var child1 = new StackPanel { Size = childSize1 };
            var child2 = new StackPanel { Size = childSize2 };
            var child3 = new StackPanel { Size = childSize3 };

            stackPanel.Children.Add(child1);
            stackPanel.Children.Add(child2);
            stackPanel.Children.Add(child3);

            stackPanel.Arrange(Vector3.Zero, false);

            // pre-arranged
            stackPanel.ScrollToEnd(Orientation.Horizontal);
            Utilities.AssertAreNearlyEqual(2 + 3 / 5f, stackPanel.ScrollPosition);

            stackPanel.ScrollToBeginning(Orientation.Horizontal);
            Utilities.AssertAreNearlyEqual(0, stackPanel.ScrollPosition);

            // post arranged
            stackPanel.InvalidateArrange();
            stackPanel.ScrollToEnd(Orientation.Horizontal);
            Utilities.AssertAreNearlyEqual(0, stackPanel.ScrollPosition);
            stackPanel.Arrange(Vector3.Zero, false);
            Utilities.AssertAreNearlyEqual(2 + 3 / 5f, stackPanel.ScrollPosition);

            stackPanel.InvalidateArrange();
            stackPanel.ScrollToBeginning(Orientation.Horizontal);
            Utilities.AssertAreNearlyEqual(2 + 3 / 5f, stackPanel.ScrollPosition);
            stackPanel.Arrange(Vector3.Zero, false);
            Utilities.AssertAreNearlyEqual(0, stackPanel.ScrollPosition);
        }

        /// <summary>
        /// Test for the <see cref="StackPanel.ScrollOf(SiliconStudio.Core.Mathematics.Vector3)"/>
        /// </summary>
        [Test]
        public void TestScrollOf()
        {
            TestScrollOf(false);
            TestScrollOf(true);
        }

        public void TestScrollOf(bool virtualizeItems)
        {
            var stackSize = new Vector3(100, 200, 300);
            var childSize1 = new Vector3(50, 150, 250);
            var childSize2 = new Vector3(150, 250, 350);
            var childSize3 = new Vector3(250, 250, 350);

            var stackPanel = new StackPanel
            {
                Size = stackSize, 
                ItemVirtualizationEnabled = virtualizeItems, 
                Orientation = Orientation.Horizontal,
                LayoutingContext = new LayoutingContext()
            };

            var child1 = new StackPanel { Size = childSize1 };
            var child2 = new StackPanel { Size = childSize2 };
            var child3 = new StackPanel { Size = childSize3 };

            stackPanel.Children.Add(child1);
            stackPanel.Children.Add(child2);
            stackPanel.Children.Add(child3);

            stackPanel.Arrange(Vector3.Zero, false);

            // pre-arranged
            stackPanel.ScrollOf(childSize1);
            Utilities.AssertAreNearlyEqual(1, stackPanel.ScrollPosition);

            stackPanel.ScrollOf(childSize1);
            Utilities.AssertAreNearlyEqual(1 + 1 / 3f, stackPanel.ScrollPosition);

            // post arranged
            stackPanel.InvalidateArrange();
            stackPanel.ScrollOf(childSize1);
            Utilities.AssertAreNearlyEqual(1 + 1 / 3f, stackPanel.ScrollPosition);
            stackPanel.Arrange(Vector3.Zero, false);
            Utilities.AssertAreNearlyEqual(1 + 2 / 3f, stackPanel.ScrollPosition);

            stackPanel.InvalidateArrange();
            stackPanel.ScrollOf(- 2 *childSize1);
            Utilities.AssertAreNearlyEqual(1 + 2 / 3f, stackPanel.ScrollPosition);
            stackPanel.Arrange(Vector3.Zero, false);
            Utilities.AssertAreNearlyEqual(1, stackPanel.ScrollPosition);
        }

        /// <summary>
        /// Test for the <see cref="StackPanel.GetSurroudingAnchorDistances"/>
        /// </summary>
        [Test]
        public void TestSurroudingAnchor()
        {
            TestSurroudingAnchor(false);
            TestSurroudingAnchor(true);
        }

        public void TestSurroudingAnchor(bool virtualizeItems)
        {
            var stackSize = new Vector3(100, 200, 300);
            var childSize1 = new Vector3(50, 150, 250);
            var childSize2 = new Vector3(150, 250, 350);
            var childSize3 = new Vector3(250, 250, 350);

            var stackPanel = new StackPanel { Size = stackSize, ItemVirtualizationEnabled = virtualizeItems, Orientation = Orientation.Horizontal };

            var child1 = new StackPanel { Size = childSize1 };
            var child2 = new StackPanel { Size = childSize2 };
            var child3 = new StackPanel { Size = childSize3 };

            stackPanel.Children.Add(child1);
            stackPanel.Children.Add(child2);
            stackPanel.Children.Add(child3);

            stackPanel.Arrange(Vector3.Zero, false);

            // checks in the scrolling direction

            stackPanel.ScrollToBeginning(Orientation.Horizontal);
            Utilities.AssertAreNearlyEqual(new Vector2(0, 50), stackPanel.GetSurroudingAnchorDistances(Orientation.Horizontal, 0));
            
            stackPanel.ScrolllToElement(0.5f);
            Utilities.AssertAreNearlyEqual(new Vector2(-25, 25), stackPanel.GetSurroudingAnchorDistances(Orientation.Horizontal, 0));

            stackPanel.ScrolllToElement(1f);
            Utilities.AssertAreNearlyEqual(new Vector2(0, 150), stackPanel.GetSurroudingAnchorDistances(Orientation.Horizontal, 0));

            stackPanel.ScrolllToElement(2 + 3/5f);
            Utilities.AssertAreNearlyEqual(new Vector2(-150, 100), stackPanel.GetSurroudingAnchorDistances(Orientation.Horizontal, 0));

            stackPanel.ScrollToEnd(Orientation.Horizontal);
            Utilities.AssertAreNearlyEqual(new Vector2(-150, 100), stackPanel.GetSurroudingAnchorDistances(Orientation.Horizontal, 0));

            // checks in other directions

            Assert.AreEqual(new Vector2(0, 200), stackPanel.GetSurroudingAnchorDistances(Orientation.Vertical, -1));
            Assert.AreEqual(new Vector2(-100, 100), stackPanel.GetSurroudingAnchorDistances(Orientation.Vertical, 100));
            Assert.AreEqual(new Vector2(-200, 0), stackPanel.GetSurroudingAnchorDistances(Orientation.Vertical, 500));

            Assert.AreEqual(new Vector2(0, 300), stackPanel.GetSurroudingAnchorDistances(Orientation.InDepth, -1));
            Assert.AreEqual(new Vector2(-150, 150), stackPanel.GetSurroudingAnchorDistances(Orientation.InDepth, 150));
            Assert.AreEqual(new Vector2(-300, 0), stackPanel.GetSurroudingAnchorDistances(Orientation.InDepth, 500));
        }

        /// <summary>
        /// Test stack panel measure function when items are virtualized.
        /// </summary>
        [Test]
        public void TestItemVirtualizedMeasure()
        {
            var measureSize = new Vector3(100, 200, 300);

            var stackPanel = new StackPanel { ItemVirtualizationEnabled = true, Orientation = Orientation.Vertical };
            stackPanel.Children.Add(new UniformGrid { Width = 10, Height = 40 });
            stackPanel.Children.Add(new UniformGrid { Width = 20, Height = 30 });
            stackPanel.Children.Add(new UniformGrid { Width = 30, Height = 20 });
            
            stackPanel.Measure(measureSize);

            Assert.AreEqual(new Vector3(30, 90, 0), stackPanel.DesiredSizeWithMargins);
        }
    }
}
