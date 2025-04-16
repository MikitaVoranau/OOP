using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Lab1_OOP;

namespace ZooApp
{
    public partial class AnimalDetailsWindow : Window
    {
        private readonly livingOrgs animal;
        private readonly Dictionary<PropertyInfo, TextBox> editors = new();
        private readonly Dictionary<Type, string> imageMap;

        public AnimalDetailsWindow(livingOrgs animal, Dictionary<Type, string> imageMap)
        {
            InitializeComponent();
            this.animal = animal;
            this.imageMap = imageMap;

            LoadImage();
            DisplayInfo();
        }

        private void LoadImage()
        {
            if (imageMap.TryGetValue(animal.GetType(), out string path))
            {
                AnimalImage.Source = new BitmapImage(new Uri(path, UriKind.Relative));
            }
        }

        private void DisplayInfo()
        {
            InfoPanel.Children.Clear();
            var props = animal.GetType().GetProperties();

            foreach (var prop in props)
            {
                if (!prop.CanWrite || !prop.CanRead) continue;

                var label = new TextBlock
                {
                    Text = Translate(prop.Name),
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 5, 0, 2)
                };

                FrameworkElement editor;
                if (prop.PropertyType == typeof(bool))
                {
                    var checkBox = new CheckBox
                    {
                        IsChecked = (bool)prop.GetValue(animal),
                        Content = "Да / Нет"
                    };
                    editor = checkBox;
                    editors[prop] = new TextBox(); // placeholder
                    editors[prop].Tag = checkBox;
                }
                else
                {
                    var textBox = new TextBox
                    {
                        Text = prop.GetValue(animal)?.ToString()
                    };
                    editor = textBox;
                    editors[prop] = textBox;
                }

                InfoPanel.Children.Add(label);
                InfoPanel.Children.Add(editor);
            }
        }

        private string Translate(string propName)
        {
            return propName switch
            {
                "Name" => "Имя",
                "Age" => "Возраст",
                "IsPredator" => "Хищник",
                "HasFur" => "Есть шерсть",
                "CanFly" => "Умеет летать",
                _ => propName
            };
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var entry in editors)
            {
                var prop = entry.Key;
                if (entry.Value.Tag is CheckBox checkBox)
                {
                    prop.SetValue(animal, checkBox.IsChecked == true);
                }
                else
                {
                    var text = entry.Value.Text;
                    try
                    {
                        object value = Convert.ChangeType(text, prop.PropertyType);
                        prop.SetValue(animal, value);
                    }
                    catch { }
                }
            }

            MessageBox.Show("Изменения сохранены!");
            Close();
        }
    }
}
