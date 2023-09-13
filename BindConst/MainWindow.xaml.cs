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



namespace BindConst
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        static string MainWindowName = "BindConstCalc";
        static string FileID = "##DataFile##";
        static int MaxSampleCount = 15;
        static int SampleCount = 15;
        TextBox[] TextBoxRatio = new TextBox[MaxSampleCount];
        TextBox[] TextBoxAbs = new TextBox[MaxSampleCount];
        Label[] LabelNumber = new Label[MaxSampleCount];
        CheckBox[] CheckBoxInclude = new CheckBox[MaxSampleCount];
        
        public MainWindow()
        {
            InitializeComponent();
            
        }

        private void MainWin_Closed(object sender, EventArgs e)
        {
            this.Close();
            Environment.Exit(1);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
            double CanvasH = CanvasCurve.Height;
            double CanvasW = CanvasCurve.Width;
            double CanvasLeft = 275;
            double CanvasTop = 30;
            CanvasCurve.Margin = new Thickness(CanvasLeft, CanvasTop, 0, 0);
            
            // Borders around CanvasCurve
            BorderCanvas.Margin = new Thickness(CanvasLeft - 5, CanvasTop - 5, 0, 0);
            BorderCanvas.Width = CanvasW + 10;
            BorderCanvas.Height = CanvasH + 10;
            BorderCanvas.BorderBrush = Brushes.Black;

            // Create TextBoxes and CheckBoxes in window
            for (int i = 0;i < MaxSampleCount; i++)
            {
                LabelNumber[i] = new Label() { Content = Convert.ToString(i+1), Width = 25, Height = 25, VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = System.Windows.HorizontalAlignment.Left };
                LabelNumber[i].Margin = new Thickness(2, 115 + i * 24, 0, 0);
                LabelNumber[i].HorizontalContentAlignment = System.Windows.HorizontalAlignment.Right;
                MainGrid.Children.Add(LabelNumber[i]);

                TextBoxRatio[i] = new TextBox() { Width = 36, Height = 19, VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = System.Windows.HorizontalAlignment.Left , TextWrapping = TextWrapping.Wrap };
                TextBoxRatio[i].Margin = new Thickness(25, 120 + i * 24, 0, 0);
                MainGrid.Children.Add(TextBoxRatio[i]);

                TextBoxAbs[i] = new TextBox() { Width = 60, Height = 19, VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = System.Windows.HorizontalAlignment.Left , TextWrapping = TextWrapping.Wrap };
                TextBoxAbs[i].Margin = new Thickness(65, 120 + i * 24, 0, 0);
                MainGrid.Children.Add(TextBoxAbs[i]);

                CheckBoxInclude[i] = new CheckBox() { IsChecked = false };
                CheckBoxInclude[i].Margin = new Thickness(128, 122 + i * 24, 0, 0);
                MainGrid.Children.Add(CheckBoxInclude[i]);
            }

            // These should be always this way
            TextBoxRatio[0].Text = "0,0";
            TextBoxRatio[0].IsEnabled = false;
            CheckBoxInclude[0].IsChecked = true;

        }
        
        private void ButtonCalculate_Click(object sender, RoutedEventArgs e)
        {

            double ConcHost = Convert.ToDouble(TextBoxConc.Text);
            double[] ConcGuest = new double[15];
            double[] Absorbace = new double[15];
            double[] HGRatio = new double[15];
            double xMax = 0; 
            int Samples = 0;


            for (int i = 0; i < 15; i++)
            {
                if (CheckBoxInclude[i].IsChecked == true && (TextBoxRatio[i].Text == "" | TextBoxAbs[i].Text == ""))
                {
                    MessageBox.Show("Fill all values or uncheck box","Error");
                    return;
                }

                try
                {
                    if (CheckBoxInclude[i].IsChecked == true)
                    {
                        Absorbace[Samples] = Convert.ToDouble(TextBoxAbs[i].Text);
                        HGRatio[Samples] = Convert.ToDouble(TextBoxRatio[i].Text);
                        ConcGuest[Samples] = HGRatio[Samples] * ConcHost;
                        if (HGRatio[Samples] > xMax) { xMax = HGRatio[Samples]; }
                        Samples++;
                    }      
                }
                catch
                {
                    MessageBox.Show("Only numerical data allowed. Check given data.", "Error");
                    return;
                }
            }

            double YDifference = 0;
            for (int i = 0; i < Samples; i++) { if (YDifference < Absorbace[i] - Absorbace[0]) { YDifference = Absorbace[i] - Absorbace[0]; } }
            double[] CalcValues = Regression(Absorbace, ConcGuest, Samples);

            DrawDataPoints(HGRatio, Absorbace, YDifference, Samples,  false, Color.FromRgb(230,10,10));
            
            DrawDataLine(CalcValues, xMax, ConcHost, Absorbace[0], YDifference);
        }

        private double[] Regression(double[] MeasuredAbs, double[] ConcGuest, int NumberOfDataPoints)
        {
            double[] BestFit = new double[2];
            double[] CalcPoint = new double[NumberOfDataPoints];
            double CalcError;
            double MinError = 999999;
            int Ka_start = 100;
            int Ka_end = 3000;
            int Ka_step = 10;
            double d_inf_start = 0.2;
            double d_inf_end = 0.0001;
            double d_inf_step = 0.0001;
            int d_direction = 1;
            if (d_inf_start > d_inf_end) { d_direction = -1; }

            for (int Ka = Ka_start; Ka <= Ka_end; Ka = Ka + Ka_step)
            {
                for (double d = d_inf_start; d >= d_inf_end; d = d + d_inf_step * d_direction)
                {
                    CalcError = 0;
                    for (int dp = 0; dp < NumberOfDataPoints; dp++)
                    {
                        CalcPoint[dp] = 1 / (1 / (Ka * ConcGuest[dp] * d) + 1 / d) + MeasuredAbs[0]; // MeasuredAbd[0] = d0
                        CalcError = CalcError + Math.Pow(CalcPoint[dp] - MeasuredAbs[dp], 2); 
                    }
                    if (CalcError < MinError)
                    {
                        MinError = CalcError;
                        BestFit[0] = Ka;
                        BestFit[1] = d;
                    }
                }
                TextBlockKa.Text = BestFit[0].ToString();
                TextBlockpKa.Text = Math.Log10(BestFit[0]).ToString("0.00");
                TextBlockInf.Text = BestFit[1].ToString("0.0000");
            }
            return BestFit;
        }
        
        private void DrawDataLine(double[] CalcValues, double XMaxVal, double C_Host, double Abs0, double Differ)
        {
            double C_Guest;
            double PointX = 0;
            double PointY;
            double Absorbance;
            double LastPointX = 0;
            double LastPointY = 0;
            double CanvasW = CanvasCurve.Width;
            double CanvasH = CanvasCurve.Height;
            double XStep = 10;
            double PointsDensity = CanvasW / XStep;
            var FillColor = new SolidColorBrush(Color.FromRgb(10,100,240));

            double Ka = CalcValues[0];
            double d_inf = CalcValues[1];
            
            double PointYSpace = (CanvasH - 10) / Differ;

            for (int i = 0; i <= PointsDensity; i++)
            {
                C_Guest = C_Host * (XMaxVal / PointsDensity * i);
                Absorbance =  1 / (1 / (Ka * C_Guest * d_inf) + 1 / d_inf) + Abs0;
                PointY = CanvasH - (Absorbance - Abs0) * PointYSpace;
                
                if (i > 0)
                {
                    Line MyLine = new Line() { X1 = LastPointX, Y1 = LastPointY, X2 = PointX, Y2 = PointY, Stroke = FillColor };
                    CanvasCurve.Children.Add(MyLine);
                }
                LastPointX = PointX;
                LastPointY = PointY;
                PointX += XStep;
            }

        }
        
        private void DrawDataPoints(double[] XDataPoints, double[] YDataPoints, double Differ, int PointsCount)
        {
            DrawDataPoints(XDataPoints, YDataPoints, Differ, PointsCount, false, Color.FromRgb(10, 10, 10));
        }

        private void DrawDataPoints(double[] XDataPoints,double[] YDataPoints, double YSpacing, int PointsCount, Boolean DrawLine, Color C)
        {
            Ellipse[] PointMeas = new Ellipse[PointsCount];
            int PointH = 5; // Ellipse point height
            int PointW = 5; // Ellipse point width
            var FillColor = new SolidColorBrush(C);

            double PointX;
            double PointY;
            double LastPointX = 0;
            double LastPointY = 0;

            double CanvasH = CanvasCurve.Height;
            double CanvasW = CanvasCurve.Width;

            // X-axis: Difference between smalles and biggest point value
            double XDiffer = 0;
            for (int i = 0; i < PointsCount; i++) { if (XDiffer < XDataPoints[i] - XDataPoints[0]) { XDiffer = XDataPoints[i] - XDataPoints[0]; } }
            double PointXSpace = (CanvasW-10)/ XDiffer;

            // Y-axis: Difference between smalles and biggest point value
            double PointYSpace = (CanvasH - 10) / YSpacing;

            for (int i = 0; i < PointsCount; i++)
            {
                PointMeas[i] = new Ellipse() { Width = PointW, Height = PointH, Fill = FillColor };
                CanvasCurve.Children.Add(PointMeas[i]);
                PointX = PointXSpace * (XDataPoints[i] - XDataPoints[0]);
                PointY = CanvasH - (YDataPoints[i] - YDataPoints[0]) * PointYSpace - PointH;
                PointMeas[i].Margin = new Thickness(PointX - PointW / 2, PointY - PointH / 2, 0, 0);
                if (DrawLine)
                {
                    if (i > 0)
                    {
                        Line MyLine = new Line() { X1 = LastPointX, Y1 = LastPointY, X2 = PointX, Y2 = PointY, Stroke = FillColor };
                        CanvasCurve.Children.Add(MyLine);

                    }
                    LastPointX = PointX;
                    LastPointY = PointY;
                }
            }
        }


        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            CanvasCurve.Children.Clear();
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            SaveDataToFile();
        }

        private void ButtonLoad_Click(object sender, RoutedEventArgs e)
        {
            
            LoadDataFromFile();
        }

        private void SaveDataToFile()
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Users\Juha\Desktop\WriteText.txt"))
            {
                file.WriteLine(FileID);
                file.WriteLine(TextBoxConc.Text);
                for (int i = 0; i < SampleCount; i++)
                {
                    file.WriteLine(TextBoxRatio[i].Text + "!" + TextBoxAbs[i].Text + "!" + CheckBoxInclude[i].IsChecked);
                }
            }
        }

        private void LoadDataFromFile()
        {
            OpenFileDialog OpenDlg = new OpenFileDialog();
            OpenDlg.ShowDialog();
            string OFile = OpenDlg.FileName;
            if (OFile == "") { return; }
            string[] lines = System.IO.File.ReadAllLines(OFile);
            string FileTitle = GetFileName(OFile);
            int LinesCount = lines.Length;

            string[] Oneline = lines[0].Split('!');
            if (Oneline[0] != FileID)
            {
                MessageBox.Show("Not a datafile or file corrupted", "File error");
                return;
            }
            Oneline = lines[1].Split('!');
            TextBoxConc.Text = Oneline[0];
            for (int i = 2; i < LinesCount-2; i++)
            {
                Oneline = lines[i].Split('!');
                TextBoxRatio[i-2].Text = Oneline[0];
                TextBoxAbs[i-2].Text = Oneline[1];
                CheckBoxInclude[i-2].IsChecked = Convert.ToBoolean(Oneline[2]);
            }
            MainWin.Title = MainWindowName + " - " + FileTitle;
        }

        private string GetFileName(string FullPath)
        {
            string[] SubStrings = FullPath.Split('\\');
            int Len = SubStrings.Length;
            return SubStrings[Len - 1];
        }
    }

}
