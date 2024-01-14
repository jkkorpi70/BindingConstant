using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Label = System.Windows.Controls.Label;
using TextBox = System.Windows.Controls.TextBox;
using CheckBox = System.Windows.Controls.CheckBox;
using MessageBox = System.Windows.Forms.MessageBox;

/*********************************************************************************************
              This is my first C# project and first bigger project after 17 years. So I had lots of things to recall and everything is not done by the best possible way.
   
   Purpose  : Calculates binding constants between molecules from measured VIS or NMR spectrometry data using Benesi–Hildebrand method.
              Calculated values are fitted to measured points using least square method.
   Author   : Juha Koivukorpi
   Date     : 22.05.2021
   Todo     : Printing
   Changes  : 
*********************************************************************************************/

namespace BindConst
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        static readonly string MainWindowName = "BindConstCalc - ";
        static readonly string FileID = "##DataFile##";
        static readonly int XAxisSpace = 20;
        static readonly int YAxisSpace = 50;
        static readonly int MaxSampleCount = 15;
        readonly TextBox[] TextBoxRatio = new TextBox[MaxSampleCount];
        readonly TextBox[] TextBoxAbs = new TextBox[MaxSampleCount];
        readonly Label[] LabelNumber = new Label[MaxSampleCount];
        readonly CheckBox[] CheckBoxInclude = new CheckBox[MaxSampleCount];
        
        public MainWindow()
        {
            InitializeComponent();
            MainWin.Title = MainWindowName;
            DrawAxeses();
        }

        private void MainWin_Closed(object sender, EventArgs e)
        {
            /*
            string Message = "Save File before exiting?";
            string BoxTitle = "Closing application.";
            DialogResult answer = MessageBox.Show(Message, BoxTitle, MessageBoxButtons.YesNo);

            switch (answer)
            {
                case System.Windows.Forms.DialogResult.Yes    : SaveDataToFile(); break;
                case System.Windows.Forms.DialogResult.No     : break;
            }*/

        }


        /**************************************************************************
         Construct main window
        **************************************************************************/
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
            double CanvasH = CanvasCurve.Height;
            double CanvasW = CanvasCurve.Width;
            double CanvasLeft = 275;
            double CanvasTop = 30;
            CanvasCurve.Margin = new Thickness(CanvasLeft, CanvasTop, 0, 0);
            CanvasCurve.Background = new SolidColorBrush(Colors.LightGray);
            
            // Borders around CanvasCurve
            BorderCanvas.Margin = new Thickness(CanvasLeft - 5, CanvasTop - 5, 0, 0);
            BorderCanvas.Width = CanvasW + 10;
            BorderCanvas.Height = CanvasH + 10;
            BorderCanvas.BorderBrush = Brushes.Black;

            // Create Labels, TextBoxes and CheckBoxes in MainWindow
            for (int i = 0; i < MaxSampleCount; i++)
            {
                LabelNumber[i] = new Label() { Content = Convert.ToString(i + 1), Width = 25, Height = 25, VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = System.Windows.HorizontalAlignment.Left };
                LabelNumber[i].Margin = new Thickness(2, 115 + i * 24, 0, 0);
                LabelNumber[i].HorizontalContentAlignment = System.Windows.HorizontalAlignment.Right;
                MainGrid.Children.Add(LabelNumber[i]);

                TextBoxRatio[i] = new TextBox() { Width = 36, Height = 19, VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = System.Windows.HorizontalAlignment.Left, TextWrapping = TextWrapping.Wrap };
                TextBoxRatio[i].Margin = new Thickness(25, 120 + i * 24, 0, 0);
                MainGrid.Children.Add(TextBoxRatio[i]);

                TextBoxAbs[i] = new TextBox() { Width = 60, Height = 19, VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = System.Windows.HorizontalAlignment.Left, TextWrapping = TextWrapping.Wrap };
                TextBoxAbs[i].Margin = new Thickness(65, 120 + i * 24, 0, 0);
                MainGrid.Children.Add(TextBoxAbs[i]);

                CheckBoxInclude[i] = new CheckBox() { IsChecked = false, VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = System.Windows.HorizontalAlignment.Left };
                CheckBoxInclude[i].Margin = new Thickness(128, 122 + i * 24, 0, 0);
                MainGrid.Children.Add(CheckBoxInclude[i]);
            }

            // These should be always this way
            TextBoxRatio[0].Text = "0";
            TextBoxRatio[0].IsEnabled = false;
            CheckBoxInclude[0].IsChecked = true;

        }
        
        private void ButtonCalculate_Click(object sender, RoutedEventArgs e)
        {
            double ConcHost;
            try
            {
                ConcHost = Convert.ToDouble(TextBoxConc.Text);
            }
            catch
            {
                MessageBox.Show("Check the value in Conc (mol/l) at 1:1 G:H"); return;
            }
            double[] ConcGuest = new double[15];
            double[] Absorbace = new double[15];
            double[] HGRatio = new double[15];
            double XMax = 0; 
            int SamplesCount = 0;


            for (int i = 0; i < 15; i++)
            {
                if (CheckBoxInclude[i].IsChecked == true && (TextBoxRatio[i].Text == "" | TextBoxAbs[i].Text == ""))
                {
                    MessageBox.Show("Fill all values or chech/uncheck the box","Error");
                    return;
                }

                try
                {
                    if (CheckBoxInclude[i].IsChecked == true)
                    {
                        Absorbace[SamplesCount] = Convert.ToDouble(TextBoxAbs[i].Text);
                        HGRatio[SamplesCount] = Convert.ToDouble(TextBoxRatio[i].Text);
                        ConcGuest[SamplesCount] = HGRatio[SamplesCount] * ConcHost;
                        if (HGRatio[SamplesCount] > XMax) { XMax = HGRatio[SamplesCount]; } 
                        SamplesCount++;
                    }      
                }
                catch
                {
                    MessageBox.Show("Only numerical data allowed. Check given data.", "Error");
                    return;
                }
            }

            if (SamplesCount < 2) { MessageBox.Show("At least 2 data points must be selected", "Insufficient data"); return; }
            
            double[] CalcValues = CalculateBinding(Absorbace, ConcGuest, SamplesCount);// CalcValues: ind[0] = ka, ind[1]=d

            TextBlockKa.Text = CalcValues[0].ToString();
            TextBlockpKa.Text = Math.Log10(CalcValues[0]).ToString("0.00");
            TextBlockInf.Text = CalcValues[1].ToString("0.0000"); // abs when [G]->inf

            CanvasCurve.Children.Clear();
            SetCanvasDimensions(XMax, Absorbace[0], CalcValues[1]); // Set canvas x and y scale
            
            DrawDataPoints(HGRatio, Absorbace, SamplesCount, Color.FromRgb(230,10,10));
            DrawDataLine(CalcValues, ConcHost, Absorbace[0]);
            DrawAxeses();
        }

        /////////////////////////////////////////////////////
        // Calculating binding constant
        /////////////////////////////////////////////////////

        private double[] CalculateBinding(double[] MeasuredAbs, double[] ConcGuest, int NumberOfDataPoints)
        {
            double[] BestFit = new double[2];
            double[] CalcPoint = new double[NumberOfDataPoints];
            double CalcError;
            double MinError = 999999;
            int Ka_start;
            int Ka_end;
            int Ka_step;
            double d_inf_start;
            double d_inf_end;
            double d_inf_step = 0.001;
            int d_direction = 1;

            // Check approximate Ka value and set parameters for calculating exact value
            double CheckBest = ScanForBestValue(MeasuredAbs, ConcGuest, NumberOfDataPoints);
            Ka_step = 10000;
            if (CheckBest < 1000000) { Ka_step = 1000; }
            if (CheckBest < 10000) { Ka_step = 10; }
            if (CheckBest < 500) { Ka_step = 1; }
            if (CheckBest > 500)
            {
                Ka_start = Convert.ToInt32(CheckBest - CheckBest / 2);
                Ka_end = Convert.ToInt32(CheckBest + CheckBest / 2);
            }
            else { Ka_start = 1; Ka_end = 500; }

            // Let's find a point which have biggest difference from point[0], for evaluating deltaA->inf
            double MaxChange = MeasuredAbs[0];
            int MaxIndex = 0;
            if (MeasuredAbs[0] < MeasuredAbs[1])
            {
                for (int i = 1; i < NumberOfDataPoints; i++)
                {
                    if (MaxChange < MeasuredAbs[i]) { MaxChange = MeasuredAbs[i]; MaxIndex = i; }
                }
                double Adiff = MaxChange - MeasuredAbs[MaxIndex - 1];
                MaxChange -= MeasuredAbs[0];
                d_inf_start = MaxChange;
                d_inf_end = MaxChange + Adiff;
            }
            else
            {
                for (int i = 1; i < NumberOfDataPoints; i++)
                {
                    if (MaxChange > MeasuredAbs[i]) { MaxChange = MeasuredAbs[i]; MaxIndex = i; }
                }

                double Adiff = MaxChange - MeasuredAbs[MaxIndex - 1];
                MaxChange -= MeasuredAbs[0];
                d_inf_start = MaxChange;
                d_inf_end = (MaxChange + Adiff);
                d_direction = -1;
            }

            // Calculate accurate binding constant
            for (int Ka = Ka_start; Ka <= Ka_end; Ka +=Ka_step)
            {
                if (d_direction == 1) // If curve is raising use this
                {
                    for (double d = d_inf_start; d <= d_inf_end; d += d_inf_step)
                    {
                        CalcError = 0;
                        for (int dp = 0; dp < NumberOfDataPoints; dp++)
                        {
                            CalcPoint[dp] = 1 / (1 / (Ka * ConcGuest[dp] * d) + 1 / d) + MeasuredAbs[0]; // MeasuredAbd[0] = d0
                            CalcError += Math.Pow(CalcPoint[dp] - MeasuredAbs[dp], 2);
                        }
                        if (CalcError < MinError)
                        {
                            MinError = CalcError;
                            BestFit[0] = Ka;
                            BestFit[1] = d;
                        }
                    }
                }
                else // This is used when curve is going down
                {
                    for (double d = d_inf_start; d >= d_inf_end; d += d_inf_step*-1)
                    {
                        CalcError = 0;
                        for (int dp = 0; dp < NumberOfDataPoints; dp++)
                        {
                            CalcPoint[dp] = 1 / (1 / (Ka * ConcGuest[dp] * d) + 1 / d) + MeasuredAbs[0]; // MeasuredAbd[0] = d0
                            CalcError += Math.Pow(CalcPoint[dp] - MeasuredAbs[dp], 2);
                        }
                        if (CalcError < MinError)
                        {
                            MinError = CalcError;
                            BestFit[0] = Ka;
                            BestFit[1] = d;
                        }
                    }
                }


            }
            return BestFit;
        }

        // Scan different areas with different step and return best ka value
        // Found area is used for more accurate Ka-value determination
        private double ScanForBestValue(double[] MeasuredAbs, double[] ConcGuest, int NumberOfDataPoints)
        {
            double[] BestFit = new double[2];
            BestFit[1] = 999999;
            double[] ReturnValues;

            ReturnValues = QuickRegression(MeasuredAbs, ConcGuest, NumberOfDataPoints, 1, 100, 1); // (100-1)/1 = 99 steps
            if (ReturnValues[1] < BestFit[1]) { BestFit[0] = ReturnValues[0]; BestFit[1] = ReturnValues[1]; }

            ReturnValues = QuickRegression(MeasuredAbs, ConcGuest, NumberOfDataPoints, 100, 10000, 10); // (10000-100)/10 = 990 steps
            if (ReturnValues[1] < BestFit[1]) { BestFit[0] = ReturnValues[0]; BestFit[1] = ReturnValues[1]; }

            ReturnValues = QuickRegression(MeasuredAbs, ConcGuest, NumberOfDataPoints, 10000, 1000000, 1000); // (1 000 000-10 000)/1000 = 990 steps
            if (ReturnValues[1] < BestFit[1]) { BestFit[0] = ReturnValues[0]; BestFit[1] = ReturnValues[1]; }

            ReturnValues = QuickRegression(MeasuredAbs, ConcGuest, NumberOfDataPoints, 1000000, 10000000, 10000); // (100 000 000-1 000 000)/10000 = 9900 steps
            if (ReturnValues[1] < BestFit[1]) { BestFit[0] = ReturnValues[0]; BestFit[1] = ReturnValues[1]; }

            return BestFit[0];
        }

        // Nonlinear regression for finding coarse value of Ka (= binding constant)
        // returns two index array where [0] = Ka and [1] = lowest error in least square method
        // Ka with lowest error is used in final calnulation

        private double[] QuickRegression(double[] MeasuredAbs, double[] ConcGuest, int NumberOfDataPoints, int Ka_start, int Ka_end, int Ka_step)
        {
            double[] BestFit = new double[2];
            double[] CalcPoint = new double[NumberOfDataPoints];
            double CalcError;
            BestFit[1] = 999999;

            double d = MeasuredAbs.Max()-MeasuredAbs[0];
            if (MeasuredAbs[NumberOfDataPoints - 1] < MeasuredAbs[0]) { d = MeasuredAbs[0] - MeasuredAbs.Min(); }

            for (int Ka = Ka_start; Ka <= Ka_end; Ka += Ka_step)
            {
                CalcError = 0;
                for (int dp = 0; dp < NumberOfDataPoints; dp++)
                {
                    CalcPoint[dp] = 1 / (1 / (Ka * ConcGuest[dp] * d) + 1 / d) + MeasuredAbs[0]; // MeasuredAbd[0] = d0
                    CalcError += Math.Pow(CalcPoint[dp] - MeasuredAbs[dp], 2);
                }
                if (CalcError < BestFit[1])
                {
                    BestFit[1] = CalcError;
                    BestFit[0] = Ka;
                }
            }
            return BestFit;
        }
        /////////////////////////////////////////////////////
        // Drawing on canvas
        /////////////////////////////////////////////////////

        private void SetCanvasDimensions(double XMax, double YMin, double YChange)
        {
            double Test1;
            double Test2;
            // Try to convert textboxes values to double. If failed, check box and use automatic values
            try
            {
                if (CheckBoxXAxis.IsChecked != true)
                { Test1 = Convert.ToDouble(TextBoxXMin.Text); Test2 = Convert.ToDouble(TextBoxXMax.Text); }

            }
            catch { CheckBoxXAxis.IsChecked = true; }

            try
            {
                if (CheckBoxYAxis.IsChecked != true)
                { Test1 = Convert.ToDouble(TextBoxYMin.Text); Test2 = Convert.ToDouble(TextBoxYMax.Text); }

            }
            catch { CheckBoxYAxis.IsChecked = true; }

            YChange += YChange * 0.05; // Add 5% space to Y-axis
            if (CheckBoxXAxis.IsChecked == true) { TextBoxXMin.Text = "0"; TextBoxXMax.Text = Convert.ToString(XMax); }
            if (YMin + YChange > YMin)// YChange can be negative
            {
                if (CheckBoxYAxis.IsChecked == true) { TextBoxYMin.Text = Convert.ToString(YMin); TextBoxYMax.Text = Convert.ToString(YMin + YChange); }
            } else
            {
                if (CheckBoxYAxis.IsChecked == true) { TextBoxYMin.Text = Convert.ToString(YMin +YChange); TextBoxYMax.Text = Convert.ToString(YMin); }

            }
        }

        // Set  x- and y-axis with values on canvas
        private void DrawAxeses()
        {

            double CanvasW = CanvasCurve.Width - YAxisSpace - 10;
            double CanvasH = CanvasCurve.Height - XAxisSpace - 10;


            Line XAxis = new Line() { X1 = YAxisSpace, Y1 = CanvasH, X2 = CanvasW + YAxisSpace, Y2 = CanvasH , Stroke = Brushes.Black };
            Line YAxis = new Line() { X1 = YAxisSpace, Y1 = 0, X2 = YAxisSpace, Y2 = CanvasH, Stroke = Brushes.Black };
            CanvasCurve.Children.Add(XAxis);
            CanvasCurve.Children.Add(YAxis);
            
            // X-axis: Difference between smalles and biggest point value. 
            double XDiffer = Convert.ToDouble(TextBoxXMax.Text) - Convert.ToDouble(TextBoxXMin.Text);
            double PointXSpace = CanvasW / XDiffer; // Pixels <-> X-values
            
            // Y-axis: Difference between smalles and biggest point value
            double YDiffer = Convert.ToDouble(TextBoxYMax.Text) - Convert.ToDouble(TextBoxYMin.Text);
            double PointYSpace = CanvasH / YDiffer; // Pixels <-> Y-values

            // X-Axis tick marks
            int XAxisTickCount = 10; // Number of ticks in X-axis
            double XAxisSpacing = XDiffer / XAxisTickCount;

            for (double i = 0; i <= XDiffer; i+=XAxisSpacing)
            {
                Line TickLine = new Line() { X1 = YAxisSpace + i * PointXSpace , Y1 = CanvasH , X2 = YAxisSpace + i * PointXSpace, Y2 = CanvasH + 10, Stroke = Brushes.Black };
                Label TickNumber = new Label() { Content = Convert.ToString(i) };
                TickNumber.Margin = new Thickness(YAxisSpace - 10 + i * PointXSpace, CanvasH + 5, 0, 0);
                CanvasCurve.Children.Add(TickNumber);
                CanvasCurve.Children.Add(TickLine);
            }

            // Y-axis tick marks
            double YMinVal = Convert.ToDouble(TextBoxYMin.Text);
            int YAxisTickCount = 10; // Number of ticks in X-axis
            double YAxisSpacing = YDiffer / YAxisTickCount;

            for (double i = 0; i <= YDiffer; i += YAxisSpacing)
            {
                Line TickLine = new Line() { X1 = YAxisSpace - 10, Y1 = CanvasH - i * PointYSpace, X2 = YAxisSpace, Y2 = CanvasH - i * PointYSpace, Stroke = Brushes.Black };
                
                Label TickNumber = new Label() { Content = (i+YMinVal).ToString("0.0000") };
                TickNumber.Margin = new Thickness(YAxisSpace - 55, CanvasH -10 - i * PointYSpace, 0, 0);
                CanvasCurve.Children.Add(TickNumber);
                CanvasCurve.Children.Add(TickLine);
            }

        }

        // Draws calculated data curve to canvas
        // CalcValues: [0] = ka, [1]=d
        private void DrawDataLine(double[] CalcValues, double C_Host, double Abs0)
        {
            double C_Guest;
            double PointX = 0;
            double PointY;
            double Absorbance;
            double LastPointX = 0;
            double LastPointY = 0;
            double CanvasW = CanvasCurve.Width - YAxisSpace - 10;
            double CanvasH = CanvasCurve.Height - XAxisSpace - 10;
            double XStep = 1;
            double PointsDensity = CanvasW / XStep;
            var FillColor = new SolidColorBrush(Color.FromRgb(10, 100, 240));
            double XMaxVal = Convert.ToDouble(TextBoxXMax.Text);
            double YDiffer = Convert.ToDouble(TextBoxYMax.Text) - Convert.ToDouble(TextBoxYMin.Text);


            double Ka = CalcValues[0];
            double d_inf = CalcValues[1];

            double CanvasXZeroPoint = Convert.ToDouble(TextBoxXMin.Text);
            double CanvasYZeroAbs = Convert.ToDouble(TextBoxYMax.Text);
            double PointYSpace = CanvasH / YDiffer;

            for (int i = 0; i <= PointsDensity; i++)
            {
                double HGRatio = (XMaxVal / PointsDensity * i);
                C_Guest = C_Host * (HGRatio + CanvasXZeroPoint);
                Absorbance = 1 / (1 / (Ka * C_Guest * d_inf) + 1 / d_inf) + Abs0;
                PointY = (CanvasYZeroAbs - Absorbance) * PointYSpace;

                    if (i > 0)
                    {
                        Line MyLine = new Line() { X1 = LastPointX + YAxisSpace, Y1 = LastPointY, X2 = PointX + YAxisSpace, Y2 = PointY, Stroke = FillColor };
                        CanvasCurve.Children.Add(MyLine);
                    }
                    LastPointX = PointX;
                    LastPointY = PointY;
                    PointX += XStep;
            }
            //Draw Line which denotes delta Abs when [G]->inf
            double YLine;
            YLine = (CanvasYZeroAbs - (Abs0 + d_inf)) * PointYSpace;
            Line InfinityLine = new Line() { X1 = YAxisSpace, Y1 = YLine, X2 = CanvasW + YAxisSpace, Y2 = YLine, Stroke = Brushes.Green };
            CanvasCurve.Children.Add(InfinityLine);
            Label InfinityLabel = new Label { Content = LabelInf.Content };
            InfinityLabel.Foreground = new SolidColorBrush(Colors.Green);
            InfinityLabel.Margin = new Thickness(YAxisSpace + 10, YLine - 20, 0, 0);
            CanvasCurve.Children.Add(InfinityLabel);

            Rectangle RectClearArea = new Rectangle { Fill = Brushes.LightGray };
            RectClearArea.Margin = new Thickness(YAxisSpace, CanvasH, 0,0);
            RectClearArea.Width = CanvasW;
            RectClearArea.Height = XAxisSpace+10;
            CanvasCurve.Children.Add(RectClearArea);
        }
        
        private void DrawDataPoints(double[] XDataPoints,double[] YDataPoints, int PointsCount, Color C)
        {

            Ellipse[] PointMeas = new Ellipse[PointsCount];
            int PointH = 5; // Ellipse point height
            int PointW = 5; // Ellipse point width
            var FillColor = new SolidColorBrush(C);
            double CanvasXZeroValue = Convert.ToDouble(TextBoxXMin.Text);
            double CanvasYZeroAbs = Convert.ToDouble(TextBoxYMax.Text);
            double PointX;
            double PointY;

            double CanvasW = CanvasCurve.Width - YAxisSpace - 10;
            double CanvasH = CanvasCurve.Height - XAxisSpace -10;

            // X-axis: Difference between smalles and biggest point value. 
            double XDiffer = Convert.ToDouble(TextBoxXMax.Text) - Convert.ToDouble(TextBoxXMin.Text);
            double PointXSpace = CanvasW / XDiffer; // Pixels <-> X-values

            // Y-axis: Difference between smalles and biggest point value
            double YDiffer = Convert.ToDouble(TextBoxYMax.Text) - Convert.ToDouble(TextBoxYMin.Text);
            double PointYSpace = CanvasH / YDiffer; // Pixels <-> Y-values

            for (int i = 0; i < PointsCount; i++)
            {
                if (XDataPoints[i] >= CanvasXZeroValue)
                {
                    PointMeas[i] = new Ellipse() { Width = PointW, Height = PointH, Fill = FillColor };
                    CanvasCurve.Children.Add(PointMeas[i]);
                    PointX = PointXSpace * (XDataPoints[i] - CanvasXZeroValue) - PointW / 2;
                    PointY = (CanvasYZeroAbs - YDataPoints[i]) * PointYSpace - PointH / 2;
                    PointMeas[i].Margin = new Thickness(PointX + YAxisSpace, PointY , 0, 0);
                }
            }
        }

        /////////////////////////////////////////////////////
        // Misc functions
        /////////////////////////////////////////////////////

        // Clear textboxes etc
        private void ClearBoxes()
        {
            DialogResult answer = MessageBox.Show("Clearing all data. Continue?", "New file?", MessageBoxButtons.YesNo);
            if (answer == System.Windows.Forms.DialogResult.No) { return; }

            for (int i= 0; i < MaxSampleCount; i++)
            {
                TextBoxRatio[i].Text = "";
                TextBoxAbs[i].Text = "";
            }
            TextBoxConc.Text = "";
            TextBoxComment.Text = "";
            TextBlockKa.Text = "0";
            TextBlockpKa.Text = "0";
            TextBlockInf.Text = "0";
            MainWin.Title = MainWindowName;

            TextBlockKa.Text = "0";
            TextBlockpKa.Text = "0";
            TextBlockInf.Text = "0";

            TextBoxRatio[0].Text = "0"; // Fix back to 0

            TextBoxXMin.Text = "0";
            TextBoxXMax.Text = "20";
            TextBoxYMin.Text = "0";
            TextBoxYMax.Text = "1";
            CanvasCurve.Children.Clear();
            DrawAxeses();
        }


        
        /////////////////////////////////////////////////////
        // File Handling
        /////////////////////////////////////////////////////
        private string SaveAsDataToFile()
        {
            SaveFileDialog SaveDlg = new SaveFileDialog { DefaultExt = "txt" };
            SaveDlg.ShowDialog();
            string SFile = SaveDlg.FileName;
            SaveDlg.Dispose();
            MainWin.Title = MainWindowName + SFile; // Change file name in title bar

            return SFile;
        }

        private void SaveDataToFile()
        {
            String SFile;
            SFile = GetFileName();
            if (SFile == "") { SFile = SaveAsDataToFile(); }
            SaveData(SFile);
        }

        private void SaveData(string SFile)
        {
            try
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(SFile))
                {
                    file.WriteLine(FileID);
                    file.WriteLine(TextBoxConc.Text);
                    for (int i = 0; i < MaxSampleCount; i++)
                    {
                        file.WriteLine(TextBoxRatio[i].Text + "!" + TextBoxAbs[i].Text + "!" + CheckBoxInclude[i].IsChecked);
                    }
                    file.WriteLine(TextBoxComment.Text);
                }
            }
            catch { MessageBox.Show("Unknown error","Error saving file"); return; }
        }

        private void LoadDataFromFile()
        {
            OpenFileDialog OpenDlg = new OpenFileDialog { Filter = "txt files(*.txt) | *.txt" };
            OpenDlg.ShowDialog();
            string OFile = OpenDlg.FileName;
            OpenDlg.Dispose();
            if (OFile == "") { return; }
            string[] lines = System.IO.File.ReadAllLines(OFile);
            int LinesCount = lines.Length;

            try
            {
                string[] Oneline = lines[0].Split('!');
                if (Oneline[0] != FileID)
                {
                    MessageBox.Show("Not a datafile or file corrupted", "File error");
                    return;
                }

                // Read data lines from file.
                Oneline = lines[1].Split('!');
                TextBoxConc.Text = Oneline[0]; // First host molecule concentration
                                               // Ratio and absorbaces
                for (int i = 2; i < LinesCount - 2; i++)
                {
                    Oneline = lines[i].Split('!');
                    TextBoxRatio[i - 2].Text = Oneline[0];
                    TextBoxAbs[i - 2].Text = Oneline[1];
                    CheckBoxInclude[i - 2].IsChecked = Convert.ToBoolean(Oneline[2]);
                }
                // Comment box
                Oneline = lines[LinesCount - 1].Split('!');
                TextBoxComment.Text = Oneline[0];
                MainWin.Title = MainWindowName + OFile;
            } catch { MessageBox.Show("Error reading file", "File Error"); return; }
            // Clear calculated values & canvas
            TextBlockKa.Text = "";
            TextBlockpKa.Text = "";
            TextBlockInf.Text = "";
            CanvasCurve.Children.Clear();
            DrawAxeses();
            CheckBoxXAxis.IsChecked = true;
            CheckBoxYAxis.IsChecked = true;
        }

        // Get filename from Main window title and returns it. If no name return empty string. 
        private string GetFileName()
        {
            string fn = MainWin.Title;
            return fn.Remove(0, MainWindowName.Length);
        }

        /* Extract file name from full file path+name string and returns it
        private string ExtractFileName(string FullPath)
        {
            string[] SubStrings = FullPath.Split('\\');
            int Len = SubStrings.Length;
            return SubStrings[Len - 1];
        }
        */

        /////////////////////////////////////////////////////
        // Button clicks
        /////////////////////////////////////////////////////

        private void ButtonNew_Click(object sender, RoutedEventArgs e) => ClearBoxes();

        private void ButtonErase_Click(object sender, RoutedEventArgs e)
        {
            CanvasCurve.Children.Clear();
            DrawAxeses();
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            SaveDataToFile();
        }

        private void ButtonLoad_Click(object sender, RoutedEventArgs e)
        {
            LoadDataFromFile();

        }

        //////////////////////////////////////////////
        // Menu Items 
        //////////////////////////////////////////////
        // File menu

        private void MenuNew_Click(object sender, RoutedEventArgs e)
        {
            ClearBoxes();    
        }

        private void MenuOpen_Click(object sender, RoutedEventArgs e)
        {
            LoadDataFromFile();
        }

        private void MenuSave_Click(object sender, RoutedEventArgs e)
        {
            SaveDataToFile();
        }

        private void MenuSaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveData(SaveAsDataToFile());
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        
    }

}
