using classes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using static classes.V2DataArray;

namespace wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 



    public class BordersToString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                double[] borders = value as double[];
                return $"{borders[0].ToString()};{borders[1].ToString()}";
            }
            
            return "";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double[] borders = new double[2];

            if (value != null)
            {
                string[] str_borders = (value as string).Split(";");
                borders[0] = System.Convert.ToDouble(str_borders[0]);
                borders[1] = System.Convert.ToDouble(str_borders[1]);
            }
            
            return borders;
        }
    }



    public class StringToInt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                return System.Convert.ToInt32(value);
            }
            catch {
                MainWindow.showError("Невозможно конвертировать значение типа string в тип int");
                return false; 
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                return value.ToString();
            }
            catch
            {
                MainWindow.showError("Невозможно конвертировать значение типа int в тип string");
                return false;
            }
        }
    }



    public class StringToDouble : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                return System.Convert.ToDouble(value);
            }
            catch
            {
                MainWindow.showError("Невозможно конвертировать значение типа string в тип double");
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                return value.ToString();
            }
            catch
            {
                MainWindow.showError("Невозможно конвертировать значение типа double в тип string");
                return false;
            }
        }
    }



    public class DoubleToSplineData : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value != null)
                {
                    double[] convert_value = value as double[];
                    return $"x: {convert_value[0]} y: {convert_value[1]}";
                }
                return "null";
            }
            catch {
                MainWindow.showError("Невозможно конвертировать значение типа double в тип SplineData");
                return false; 
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return false;
        }
    }



    public partial class MainWindow : Window
    {
        public ViewData viewData = new ViewData();

        public MainWindow()
        {
            InitializeComponent();      
        }

        private void loadedWindowHandler(object sender, RoutedEventArgs e)
        {
            function.ItemsSource = viewData.functions_titles;
            DataContext = viewData;
        }

        private void saveDataHandler(object sender, System.EventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "data";
            dlg.DefaultExt = ".txt";
            dlg.Filter = "Text documents (.txt)|*.txt";

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    viewData.Save(dlg.FileName);
                    showMessage("Данные успешно сохранены");
                }
                catch (Exception exc)
                {
                    showError("Ошибка сохранения данных в файл " + dlg.FileName + ": " + exc);
                }
            }
            else
            {
                showError("Файл " + dlg.FileName + " не найден");
            }
        }

        private void loadDataHandler(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "data";
            dlg.DefaultExt = ".txt";
            dlg.Filter = "Text documents (.txt)|*.txt";

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    viewData.Load(dlg.FileName);
                    BindingOperations.GetBindingExpression(borders, TextBox.TextProperty).UpdateTarget();
                    BindingOperations.GetBindingExpression(nodes, TextBox.TextProperty).UpdateTarget();
                    function.SelectedIndex = viewData.DataArray.FunctionIndex;

                    if (viewData.DataArray.GridType == true)
                    {
                        gridtype.SelectedIndex = 0;
                    } 
                    else
                    {
                        gridtype.SelectedIndex = 1;
                    }

                    DataFromControls.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));


                }
                catch (Exception exc)
                {
                    showError("Ошибка загрузки данных из файла " + dlg.FileName + ": " + exc);
                }
            }
            else
            {
                showError("Файл " + dlg.FileName + " не найден");
            }
        }

        private void dataFromControlsHandler(object sender, RoutedEventArgs e)
        {
            try
            {
                if (viewData.borders[0] >= viewData.borders[1] || viewData.borders[0] == double.NaN || viewData.borders[1] == double.NaN) { 
                    showError("Некоррекное значение границ"); 
                    return;
                }

                if (viewData.nodes < 2) {
                    showError("Некоррекное значение узлов"); 
                    return; 
                }

                viewData.getDataArray();

                if (viewData.load_data)
                {
                    try
                    {
                      
                        if (viewData.splinenodes < 1 || viewData.splinenodes > viewData.nodes)
                        {
                            showError("Некоррекное значение узлов сплайна");
                            return;
                        }

                        if (viewData.smallsplinenodes < 3)
                        {
                            showError("Некоррекное значение узлов равномерной сетки");
                            return;
                        }

                        if (viewData.iterations <= 0)
                        {
                            showError("Некоррекное значение итераций");
                            return;
                        }

                        if (viewData.residualnorm <= 0) 
                        {
                            showError("Некоррекное значение нормы невязки");
                            return;
                        }

                        //viewData.toString();

                        viewData.getSplineData();
                        SplineDataItemOut.ItemsSource = viewData.SplineData.Results;
                        SplineDataDoubleMeshOut.ItemsSource = viewData.SplineData.ResultsSmallGrid;
                    }
                    catch (Exception exp)
                    {
                        showError("Ошибка при построении сплайна: " + exp);
                    }
                }
                else
                {
                    showError("Данные не загружены");
                }
            }
            catch
            {
                showError("Ошибка при обработке данных");
            }
        }

        private void enableSaveHandler(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;

            if (viewData != null)
            {
                e.CanExecute = viewData.load_data;
            }
        }

        private void gridtypeSelectHandler(object sender, SelectionChangedEventArgs e)
        {
            viewData.gridtype = false;

            if (gridtype.SelectedIndex == 0)
            {
                viewData.gridtype = true;
            }
        }

        private void functionSelectHandler(object sender, SelectionChangedEventArgs e)
        {
            viewData.function = function.SelectedIndex;
        }

        public static void showError(string msg)
        {
            MessageBox.Show(msg, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public static void showMessage(string msg)
        {
            MessageBox.Show(msg, "Сообщение", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }



    public static class CustomCommands
    {
        public static RoutedCommand SaveCom = new RoutedCommand("SaveCom", typeof(CustomCommands));
        public static RoutedCommand ExecuteCom = new RoutedCommand("ExecuteCom", typeof(CustomCommands));
    }



    public class ViewData
    {
        public double[] borders { get; set; }
        public int nodes { get; set; }
        public bool gridtype { get; set; }
        public int function { get; set; }
        public ObservableCollection<string> functions_titles;
        public ObservableCollection<FValues> functions;
        public int splinenodes { get; set; }
        public int smallsplinenodes { get; set; }
        public double residualnorm { get; set; }
        public int iterations { get; set; }
        public bool load_data = false;

        public classes.V2DataArray DataArray;
        public classes.SplineData SplineData;

        public List<SplineDataItem> splinedatageter1 { 
            get {
                if (SplineData != null)
                {
                    return SplineData.Results;
                }

                return null; 
            } 
            set { } 
        }

        public List<double[]> splinedatageter2 {
            get
            {
                if (SplineData != null)
                {
                    return SplineData.ResultsSmallGrid;
                }

                return null;
            }
            set { } 
        }

        static double function1(double x, int index)
        {
            return x * x;
        }

        static double function2(double x, int index)
        {
            return x * x * x;
        }


        static double function3(double x, int index)
        {
            if (index == 0)
            {
                return (1 / 2 * x) + 2;
            }

            return (1 / 2 * x) + 4;
        }

        static double function4(double x, int index)
        {
            if (index == 0)
            {
                return (x * 4 + x * x + 2) / 2;
            }

            return (x * 4 + x * x + 4) / 4;
        }

        public ViewData()
        {
            borders = new double[2];
            functions_titles = new ObservableCollection<string>()
            {
                "x^2",
                "x^3",
                "(1 / 2 * x) + 2",
                "(x * 4 + x^2 + 2) / 2"
            };

            functions = new ObservableCollection<FValues>()
            {
                function1,
                function2,
                function3,
                function4
            };
            load_data = false;
        }

        public ViewData(double[] borders, int nodes, bool gridtype, int splinenodes, int smallsplinenodes, FValues function)
        {
            this.borders = new double[2];
            this.borders = borders;
            this.nodes = nodes;
            this.gridtype = gridtype;
            this.splinenodes = splinenodes;
            this.smallsplinenodes = smallsplinenodes;
            this.DataArray = new V2DataArray("DataArray1", DateTime.Now, nodes, borders[0], borders[1], function, gridtype, this.function);
        }

        public void Save(string filename)
        {
            V2DataArray.Save(filename, this.DataArray);
        }

        public void Load(string filename)
        {
            this.DataArray = new V2DataArray();
            classes.V2DataArray.Load(filename, ref this.DataArray);
            this.load_data = true;
            this.borders[0] = this.DataArray.Borders[0];
            this.borders[1] = this.DataArray.Borders[1];
            this.nodes = this.DataArray.X.Length;
        }

        public void getDataArray()
        {
            if (gridtype == true)
            {
                this.DataArray = new V2DataArray("DataArray1", DateTime.Now, nodes, borders[0], borders[1], functions[function], gridtype, function);
            }
            else
            {
                double[] nodes = new double[this.nodes];

                for (int i = 0; i < nodes.Length; i++)
                {
                    Random random = new Random();
                    nodes[i] = random.NextDouble() * (this.borders[1] - this.borders[0]) + this.borders[0];
                }

                Array.Sort(nodes);
                
                this.DataArray = new V2DataArray("DataArray1", DateTime.Now, new double[2] { borders[0], borders[1] }, nodes, functions[function], gridtype, function);
            }

            load_data = true;
        }

        public void getSplineData()
        {
            this.SplineData = new classes.SplineData(this.DataArray, splinenodes, smallsplinenodes, residualnorm, iterations);
            this.SplineData.ApproximateSpline();
        }

        public void toString()
        {
            Debug.WriteLine("------------- ViewData -------------");
            Debug.WriteLine("borders: " + this.borders[0] + " | " + this.borders[1]);
            Debug.WriteLine("nodes: " + this.nodes);
            Debug.WriteLine("gridtype: " + this.gridtype);
            Debug.WriteLine("function: " + this.functions_titles[this.function]);
            Debug.WriteLine("splinenodes: " + this.splinenodes);
            Debug.WriteLine("smallsplinenodes: " + this.smallsplinenodes);
            Debug.WriteLine("residualnorm: " + this.residualnorm);
            Debug.WriteLine("iterations: " + this.iterations);
            Debug.WriteLine("------------------------------------");
        }
    }
}
