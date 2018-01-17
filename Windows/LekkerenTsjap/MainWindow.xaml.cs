﻿using MahApps.Metro.Controls;
using Microsoft.Win32;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace LekkerenTsjap
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow, INotifyPropertyChanged
    {
        private DateTime startTime;
        private static List<MeasureModel> Values = new List<MeasureModel>();
        private static List<MeasureModel> GoalValues = new List<MeasureModel>();
        private double _temperature;
        private double _goal;
        private double _goalTempStartAngle;
        private double _goalTempEndAngle;
        private double _goalTempAngle;
        private double _currentTempAngle;
        private BackgroundWorker _readWorker = new BackgroundWorker();
        private BackgroundWorker _recordingWorker = new BackgroundWorker();
        private bool _record = false;
        private bool _pwmOn = false;
        private bool _arduinoFound = false;
        private string _recordText = "Start Recording";
        private TimeSpan _recordingTime = new TimeSpan();

        private Geometry _geometry;

        public Geometry Geometry
        {
            get { return _geometry; }
            set
            {
                _geometry = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("SelectedGeometry"));
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            Geometry = this.Resources["PathGeometry"] as PathGeometry;
            _readWorker.DoWork += _readWorker_DoWork;
            _readWorker.WorkerSupportsCancellation = true;
            _readWorker.RunWorkerAsync();
            _recordingWorker.DoWork += _recordingWorker_DoWork;
            _recordingWorker.WorkerSupportsCancellation = true;
        }

        private void _recordingWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!_recordingWorker.CancellationPending)
            {
                RecordingTime = DateTime.Now - startTime;
            }
        }

        private void _readWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!_readWorker.CancellationPending)
            {
                Read();
            }
        }

        public double Temperature
        {
            get { return _temperature; }
            set
            {
                _temperature = value;
                CurrentTempAngle = (value * 2.4) - 120;
                OnPropertyChanged("Temperature");
            }
        }

        public double Goal
        {
            get { return _goal; }
            set
            {
                _goal = value;
                GoalTempAngle = value * 2.4;
                GoalTempStartAngle = (value * 2.4) - 120;
                GoalTempEndAngle = (value * 2.4) - 119.5;
                OnPropertyChanged("Goal");
            }
        }

        public double GoalTempStartAngle { get => _goalTempStartAngle; set { _goalTempStartAngle = value; OnPropertyChanged("GoalTempStartAngle"); } }
        public double GoalTempEndAngle { get => _goalTempEndAngle; set { _goalTempEndAngle = value; OnPropertyChanged("GoalTempEndAngle"); } }
        public double GoalTempAngle { get => _goalTempAngle; set { _goalTempAngle = value; OnPropertyChanged("GoalTempAngle"); } }
        public double CurrentTempAngle { get => _currentTempAngle; set { _currentTempAngle = value; OnPropertyChanged("CurrentTempAngle"); } }

        public bool Record { get => _record; set { _record = value; OnPropertyChanged("Record"); } }

        public string RecordText { get => _recordText; set { _recordText = value; OnPropertyChanged("RecordText"); } }

        public TimeSpan RecordingTime { get => _recordingTime; set { _recordingTime = value; OnPropertyChanged("RecordingTime"); } }

        public bool PwmOn { get => _pwmOn; set { _pwmOn = value; OnPropertyChanged("PwmOn"); } }

        public bool ArduinoFound { get => _arduinoFound; set { _arduinoFound = value; OnPropertyChanged("ArduinoFound"); } }

        private void Read()
        {
            try
            {
                var now = DateTime.Now;
                string[] info = ArduinoApi.GetAllInfo().Split(':');
                Temperature = Math.Round(double.Parse(info[0], CultureInfo.InvariantCulture));
                Goal = Math.Round(double.Parse(info[1], CultureInfo.InvariantCulture));
                PwmOn = bool.Parse(info[2]);
                Thread.Sleep(100);
                if (Record)
                {
                    Values.Add(new MeasureModel
                    {
                        DateTime = now,
                        Value = Temperature
                    });
                    GoalValues.Add(new MeasureModel
                    {
                        DateTime = now,
                        Value = Goal
                    });
                }
            }
            catch
            {
            }
        }

        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged implementation

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!Record)
            {
                Record = true;
                RecordText = "Stop Recording";
                startTime = DateTime.Now;
                _recordingWorker.RunWorkerAsync();
            }
            else
            {
                _recordingWorker.CancelAsync();
                Record = false;
                RecordText = "Start Recording";
                _readWorker.CancelAsync();
                SaveFileDialog dialog = new SaveFileDialog();
                dialog.AddExtension = true;
                dialog.DefaultExt = "xlsx";
                dialog.Filter = "Excel Files (*.xlsx)|*.xlsx";
                dialog.FileName = "Brouwdata van " + DateTime.Now.ToString("dd-MM-yyyy") + ".xlsx";
                if (dialog.ShowDialog() == true)
                {
                    GenerateXlsx(dialog.FileName);
                }
            }
        }

        private void GenerateXlsx(string fileName)
        {
            if (File.Exists(fileName)) { File.Delete(fileName); }
            var newFile = new FileInfo(fileName);
            using (ExcelPackage xlPackage = new ExcelPackage(newFile))
            {
                ExcelWorksheet worksheet = xlPackage.Workbook.Worksheets.Add("Data");
                ExcelWorksheet graphworksheet = xlPackage.Workbook.Worksheets.Add("Graph");
                worksheet.Cells.Clear();
                worksheet.Cells["A1"].Value = "Tijd";
                worksheet.Cells["B1"].Value = "Gemeten temperatuur";
                worksheet.Cells["C1"].Value = "Te bereiken temperatuur";
                for (int row = 0; row < Values.Count; row++)
                {
                    worksheet.Cells["A" + (row + 2)].Value = Values[row].DateTime.ToString("HH:mm:ss");
                    worksheet.Cells["B" + (row + 2)].Value = Values[row].Value;
                    worksheet.Cells["C" + (row + 2)].Value = GoalValues[row].Value;
                }
                ExcelChart chart = graphworksheet.Drawings.AddChart("Brouwdata", eChartType.LineStacked);
                chart.Title.Text = "Brouw Data";
                chart.SetPosition(0, 0, 0, 0);
                chart.SetSize(1400, 600);
                var ser1 = (ExcelChartSerie)(chart.Series.Add(worksheet.Cells["B2:B" + (Values.Count + 1)],
                worksheet.Cells["A2:A" + (Values.Count + 1)]));
                var ser2 = (ExcelChartSerie)(chart.Series.Add(worksheet.Cells["C2:C" + (Values.Count + 1)],
                worksheet.Cells["A2:A" + (Values.Count + 1)]));
                xlPackage.SaveAs(newFile);
            }
            Process.Start(fileName);
        }
    }

    public class MeasureModel
    {
        public DateTime DateTime { get; set; }
        public double Value { get; set; }
    }

    [ValueConversion(typeof(bool), typeof(bool))]
    public class InvertBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool booleanValue = (bool)value;
            return !booleanValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool booleanValue = (bool)value;
            return !booleanValue;
        }
    }
}