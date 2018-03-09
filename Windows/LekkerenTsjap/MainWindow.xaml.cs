using MahApps.Metro.Controls;
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
        private static string _serverFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "server");
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
        private bool _arduinoError = false;
        private bool _setTimestamp = false;
        private bool _saveEnabled = false;
        private string _recordText = "Start Recording";
        private string _serverAddress = "";
        private TimeSpan _recordingTime = new TimeSpan();
        private long _dataPoints = 0;
        private bool _initializing = true;

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
            try
            {
                ServerAddress = File.ReadAllText(_serverFile);
            }
            catch
            {
                ArduinoError = true;
            }
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
                Thread.Sleep(50);
            }
        }

        private void _readWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!_readWorker.CancellationPending)
            {
                Read();
                Thread.Sleep(50);
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

        public bool ArduinoError { get => _arduinoError; set { _arduinoError = value; OnPropertyChanged("ArduinoError"); } }

        public long DataPoints { get => _dataPoints; set { _dataPoints = value; OnPropertyChanged("DataPoints"); } }

        public bool SaveEnabled { get => _saveEnabled; set { _saveEnabled = value; OnPropertyChanged("SaveEnabled"); } }

        public string ServerAddress { get => _serverAddress; set { _serverAddress = value; ArduinoApi.baseUrl = value; File.WriteAllText(_serverFile, value); Initializing = true; OnPropertyChanged("ServerAddress"); } }

        public bool Initializing { get => _initializing; set { _initializing = value; OnPropertyChanged("Initializing"); } }

        private void Read()
        {
            try
            {
                var now = DateTime.Now;
                string[] info = ArduinoApi.GetAllInfo().Split(':');
                Temperature = Math.Round(double.Parse(info[0], CultureInfo.InvariantCulture));
                Goal = Math.Round(double.Parse(info[1], CultureInfo.InvariantCulture));
                PwmOn = bool.Parse(info[2]);
                if (Record)
                {
                    Values.Add(new MeasureModel
                    {
                        DateTime = now,
                        Value = Temperature,
                        Timestamp = _setTimestamp ? (double?)Temperature : (double?)null
                    });
                    GoalValues.Add(new MeasureModel
                    {
                        DateTime = now,
                        Value = Goal,
                        Timestamp = _setTimestamp ? (double?)Temperature : (double?)null
                    });
                    _setTimestamp = false;
                    DataPoints++;
                }
                ArduinoError = false;
            }
            catch
            {
                ArduinoError = true;
            }
            Initializing = false;
        }

        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged implementation

        private void ToggleRecording(object sender, RoutedEventArgs e)
        {
            if (!Record)
            {
                SaveEnabled = true;
                Record = true;
                RecordText = "Pause Recording";
                startTime = DateTime.Now;
                _recordingWorker.RunWorkerAsync();
            }
            else
            {
                _recordingWorker.CancelAsync();
                Record = false;
                RecordText = "Continue Recording";
            }
        }

        private void SaveDataToXls()
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = "xlsx",
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = "Brouwdata van " + DateTime.Now.ToString("dd-MM-yyyy") + ".xlsx"
            };
            if (dialog.ShowDialog() == true)
            {
                GenerateXlsx(dialog.FileName);
            }
        }

        private void GenerateXlsx(string fileName)
        {
            if (Values.Count > 0)
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
                    worksheet.Cells["D1"].Value = "Timestamp";
                    for (int row = 0; row < Values.Count; row++)
                    {
                        worksheet.Cells["A" + (row + 2)].Value = Values[row].DateTime.ToString("HH:mm:ss");
                        worksheet.Cells["B" + (row + 2)].Value = Values[row].Value;
                        worksheet.Cells["C" + (row + 2)].Value = GoalValues[row].Value;
                        worksheet.Cells["D" + (row + 2)].Value = Values[row].Timestamp;
                    }
                    ExcelChart chart = graphworksheet.Drawings.AddChart("Brouwdata", eChartType.Line);
                    chart.Title.Text = "Brouw Data " + DateTime.Now.ToString("dd-MM-yyyy");
                    chart.SetPosition(0, 0, 0, 0);
                    chart.SetSize(1400, 600);
                    chart.DisplayBlanksAs = eDisplayBlanksAs.Gap;
                    chart.Legend.Remove();
                    var ser1 = chart.Series.Add(worksheet.Cells["B2:B" + (Values.Count + 1)], worksheet.Cells["A2:A" + (Values.Count + 1)]);
                    var ser2 = chart.Series.Add(worksheet.Cells["C2:C" + (Values.Count + 1)], worksheet.Cells["A2:A" + (Values.Count + 1)]);

                    var chartType2 = chart.PlotArea.ChartTypes.Add(eChartType.XYScatter);
                    var ser3 = chartType2.Series.Add(worksheet.Cells["D2:D" + (Values.Count + 1)], worksheet.Cells["A2:A" + (Values.Count + 1)]);

                    xlPackage.SaveAs(newFile);
                }
                Process.Start(fileName);
            }
        }

        private void SetTimestamp(object sender, RoutedEventArgs e)
        {
            _setTimestamp = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _readWorker.CancelAsync();
            if (Record)
            {
                ToggleRecording(null, new RoutedEventArgs());
            }
            SaveDataToXls();
            while (_readWorker.IsBusy) { }
            _readWorker.RunWorkerAsync();
        }
    }

    public class MeasureModel
    {
        public DateTime DateTime { get; set; }
        public double Value { get; set; }
        public double? Timestamp { get; set; }
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