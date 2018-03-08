using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Markup;

namespace TextOnAPath
{
    [ContentProperty("Text")]
    public class TextOnAPath : Control
    {
        static TextOnAPath()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TextOnAPath), new FrameworkPropertyMetadata(typeof(TextOnAPath)));

            Control.FontSizeProperty.OverrideMetadata(typeof(TextOnAPath),
                new FrameworkPropertyMetadata(
                    new PropertyChangedCallback(OnFontPropertyChanged)));

            Control.FontFamilyProperty.OverrideMetadata(typeof(TextOnAPath),
                new FrameworkPropertyMetadata(
                    new PropertyChangedCallback(OnFontPropertyChanged)));

            Control.FontStretchProperty.OverrideMetadata(typeof(TextOnAPath),
                new FrameworkPropertyMetadata(
                    new PropertyChangedCallback(OnFontPropertyChanged)));

            Control.FontStyleProperty.OverrideMetadata(typeof(TextOnAPath),
                new FrameworkPropertyMetadata(
                    new PropertyChangedCallback(OnFontPropertyChanged)));

            Control.FontWeightProperty.OverrideMetadata(typeof(TextOnAPath),
                new FrameworkPropertyMetadata(
                    new PropertyChangedCallback(OnFontPropertyChanged)));
        }

        static void OnFontPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TextOnAPath textOnAPath = d as TextOnAPath;

            if (textOnAPath == null)
                return;

            if (e.NewValue == null || e.NewValue == e.OldValue)
                return;

            textOnAPath.UpdateText();
            textOnAPath.Update();
        }

        double[] _segmentLengths;
        TextBlock[] _textBlocks;

        Panel _layoutPanel;
        bool _layoutHasValidSize = false;

        #region Text DP
        public String Text
        {
            get { return (String)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Text.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(String), typeof(TextOnAPath),
            new PropertyMetadata(null, new PropertyChangedCallback(OnStringPropertyChanged),
                new CoerceValueCallback(CoerceTextValue)));

        static void OnStringPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TextOnAPath textOnAPath = d as TextOnAPath;

            if (textOnAPath == null)
                return;

            if (e.NewValue == e.OldValue || e.NewValue == null)
            {
                if (textOnAPath._layoutPanel != null)
                    textOnAPath._layoutPanel.Children.Clear();
                return;
            }

            textOnAPath.UpdateText();
            textOnAPath.Update();
        }

        static object CoerceTextValue(DependencyObject d, object baseValue)
        {
            if ((String)baseValue == "")
                return null;

            return baseValue;
        }

        #endregion

        #region TextPath DP
        public Geometry TextPath
        {
            get { return (Geometry)GetValue(TextPathProperty); }
            set { SetValue(TextPathProperty, value); }
        }

        public static readonly DependencyProperty TextPathProperty =
            DependencyProperty.Register("TextPath", typeof(Geometry), typeof(TextOnAPath),
            new FrameworkPropertyMetadata(null,

                                          new PropertyChangedCallback(OnTextPathPropertyChanged)));

        static void OnTextPathPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TextOnAPath textOnAPath = d as TextOnAPath;

            if (textOnAPath == null)
                return;

            if (e.NewValue == e.OldValue || e.NewValue == null)
                return;

            textOnAPath.TextPath.Transform = null;

            textOnAPath.UpdateSize();
            textOnAPath.Update();
        }

        #endregion

        #region DrawPath DP

        /// <summary>
        /// Set this property to True to display the TextPath geometry in the control
        /// </summary>
        public bool DrawPath
        {
            get { return (bool)GetValue(DrawPathProperty); }
            set { SetValue(DrawPathProperty, value); }
        }

        public static readonly DependencyProperty DrawPathProperty =
            DependencyProperty.Register("DrawPath", typeof(bool), typeof(TextOnAPath),
            new PropertyMetadata(false, new PropertyChangedCallback(OnDrawPathPropertyChanged)));

        static void OnDrawPathPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TextOnAPath textOnAPath = d as TextOnAPath;

            if (textOnAPath == null)
                return;

            if (e.NewValue == e.OldValue || e.NewValue == null)
                return;

            textOnAPath.Update();
        }

        #endregion

        #region DrawLinePath DP
        /// <summary>
        /// Set this property to True to display the line segments under the text (flattened path)
        /// </summary>
        public bool DrawLinePath
        {
            get { return (bool)GetValue(DrawLinePathProperty); }
            set { SetValue(DrawLinePathProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DrawFlattendPath.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DrawLinePathProperty =
            DependencyProperty.Register("DrawLinePath", typeof(bool), typeof(TextOnAPath),
            new PropertyMetadata(false, new PropertyChangedCallback(OnDrawLinePathPropertyChanged)));

        static void OnDrawLinePathPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TextOnAPath textOnAPath = d as TextOnAPath;

            if (textOnAPath == null)
                return;

            if (e.NewValue == e.OldValue || e.NewValue == null)
                return;

            textOnAPath.Update();
        }

        #endregion

        #region ScaleTextPath DP
        /// <summary>
        /// If set to True (default) then the geometry defined by TextPath automatically gets scaled to fit the width/height of the control
        /// </summary>
        public bool ScaleTextPath
        {
            get { return (bool)GetValue(ScaleTextPathProperty); }
            set { SetValue(ScaleTextPathProperty, value); }
        }

        public static readonly DependencyProperty ScaleTextPathProperty =
            DependencyProperty.Register("ScaleTextPath", typeof(bool), typeof(TextOnAPath),
                    new PropertyMetadata(false, new PropertyChangedCallback(OnScaleTextPathPropertyChanged)));

        static void OnScaleTextPathPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TextOnAPath textOnAPath = d as TextOnAPath;

            if (textOnAPath == null)
                return;

            if (e.NewValue == e.OldValue)
                return;

            bool value = (Boolean)e.NewValue;

            if (value == false && textOnAPath.TextPath != null)
                textOnAPath.TextPath.Transform = null;

            textOnAPath.UpdateSize();
            textOnAPath.Update();

        }

        #endregion

        void UpdateText()
        {
            if (Text == null || FontFamily == null || FontWeight == null || FontStyle == null)
                return;

            _textBlocks = new TextBlock[Text.Length];
            _segmentLengths = new double[Text.Length];

            for (int i = 0; i < Text.Length; i++)
            {
                TextBlock t = new TextBlock();
                t.FontSize = this.FontSize;
                t.FontFamily = this.FontFamily;
                t.FontStretch = this.FontStretch;
                t.FontWeight = this.FontWeight;
                t.FontStyle = this.FontStyle;
                t.Text = new String(Text[i], 1);
                t.RenderTransformOrigin = new Point(0.0, 1.0);

                t.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

                _textBlocks[i] = t;
                _segmentLengths[i] = t.DesiredSize.Width;


            }
        }

        void Update()
        {
            if (Text == null || TextPath == null || _layoutPanel == null || !_layoutHasValidSize)
                return;

            List<Point> intersectionPoints;

            intersectionPoints = GeometryHelper.GetIntersectionPoints(TextPath.GetFlattenedPathGeometry(), _segmentLengths);

            _layoutPanel.Children.Clear();

            _layoutPanel.Margin = new Thickness(FontSize);

            for (int i = 0; i < intersectionPoints.Count - 1; i++)
            {
                double oppositeLen = Math.Sqrt(Math.Pow(intersectionPoints[i].X + _segmentLengths[i] - intersectionPoints[i + 1].X, 2.0) + Math.Pow(intersectionPoints[i].Y - intersectionPoints[i + 1].Y, 2.0)) / 2.0;
                double hypLen = Math.Sqrt(Math.Pow(intersectionPoints[i].X - intersectionPoints[i + 1].X, 2.0) + Math.Pow(intersectionPoints[i].Y - intersectionPoints[i + 1].Y, 2.0));

                double ratio = oppositeLen / hypLen;

                if (ratio > 1.0)
                    ratio = 1.0;
                else if (ratio < -1.0)
                    ratio = -1.0;

                //double angle = 0.0;

                double angle = 2.0 * Math.Asin(ratio) * 180.0 / Math.PI;

                // adjust sign on angle
                if ((intersectionPoints[i].X + _segmentLengths[i]) > intersectionPoints[i].X)
                {
                    if (intersectionPoints[i + 1].Y < intersectionPoints[i].Y)
                        angle = -angle;
                }
                else
                {
                    if (intersectionPoints[i + 1].Y > intersectionPoints[i].Y)
                        angle = -angle;
                }

                TextBlock currTextBlock = _textBlocks[i];

                RotateTransform rotate = new RotateTransform(angle);
                TranslateTransform translate = new TranslateTransform(intersectionPoints[i].X, intersectionPoints[i].Y - currTextBlock.DesiredSize.Height);
                TransformGroup transformGrp = new TransformGroup();
                transformGrp.Children.Add(rotate);
                transformGrp.Children.Add(translate);
                currTextBlock.RenderTransform = transformGrp;

                _layoutPanel.Children.Add(currTextBlock);

                if (DrawLinePath == true)
                {
                    Line line = new Line();
                    line.X1 = intersectionPoints[i].X;
                    line.Y1 = intersectionPoints[i].Y;
                    line.X2 = intersectionPoints[i + 1].X;
                    line.Y2 = intersectionPoints[i + 1].Y;
                    line.Stroke = Brushes.Black;
                    _layoutPanel.Children.Add(line);
                }
            }

            // don't draw path if already drawing line path
            if (DrawPath == true && DrawLinePath == false)
            {
                Path path = new Path();
                path.Data = TextPath;
                path.Stroke = Brushes.Black;
                _layoutPanel.Children.Add(path);
            }
        }

        public TextOnAPath()
        {
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _layoutPanel = GetTemplateChild("LayoutPanel") as Panel;
            if (_layoutPanel == null)
                throw new Exception("Could not find template part: LayoutPanel");

            _layoutPanel.SizeChanged += new SizeChangedEventHandler(_layoutPanel_SizeChanged);
        }

        Size _newSize;

        void _layoutPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _newSize = e.NewSize;

            UpdateSize();
            Update();
        }
        
        void UpdateSize()
        {
            if (_newSize == null || TextPath == null)
                return;

            _layoutHasValidSize = true;

            double xScale = _newSize.Width / TextPath.Bounds.Width;
            double yScale = _newSize.Height / TextPath.Bounds.Height;

            if (TextPath.Bounds.Width <= 0)
                xScale = 1.0;

            if (TextPath.Bounds.Height <= 0)
                xScale = 1.0;

            if (xScale <= 0 || yScale <= 0)
                return;

            if (TextPath.Transform is TransformGroup)
            {
                TransformGroup grp = TextPath.Transform as TransformGroup;
                if (grp.Children[0] is ScaleTransform && grp.Children[1] is TranslateTransform)
                {
                    if (ScaleTextPath)
                    {
                        ScaleTransform scale = grp.Children[0] as ScaleTransform;
                        scale.ScaleX *= xScale;
                        scale.ScaleY *= yScale;
                    }

                    TranslateTransform translate = grp.Children[1] as TranslateTransform;
                    translate.X += -TextPath.Bounds.X;
                    translate.Y += -TextPath.Bounds.Y;
                }
            }
            else
            {
                ScaleTransform scale;
                TranslateTransform translate;

                if (ScaleTextPath)
                {
                    scale = new ScaleTransform(xScale, yScale);
                    translate = new TranslateTransform(-TextPath.Bounds.X * xScale, -TextPath.Bounds.Y * yScale);
                }
                else
                {
                    scale = new ScaleTransform(1.0, 1.0);
                    translate = new TranslateTransform(-TextPath.Bounds.X, -TextPath.Bounds.Y );
                }

                TransformGroup grp = new TransformGroup();
                grp.Children.Add(scale);
                grp.Children.Add(translate);
                TextPath.Transform = grp;
            }
        }
    }
}
