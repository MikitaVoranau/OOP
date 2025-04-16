using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Lab1_OOP;

namespace ZooApp
{
    public partial class MainWindow : Window
    {
        private readonly List<livingOrgs> animals = new();
        private readonly Dictionary<Type, string> imageMap = new();
        private readonly Dictionary<string, FrameworkElement> fieldInputs = new();
        private ConstructorInfo? currentConstructor;

        public MainWindow()
        {
            InitializeComponent();
            LoadAnimalTypes();
            SetupImageMap();
        }

        private void LoadAnimalTypes()
        {
            var types = Assembly.Load("Lab1_OOP")
                .GetTypes()
                .Where(t => typeof(livingOrgs).IsAssignableFrom(t) && !t.IsAbstract)
                .ToList();

            TypeComboBox.ItemsSource = types;
        }

        private void SetupImageMap()
        {
            imageMap[typeof(Carnivore)] = "Images/carnivore.png";
            imageMap[typeof(BirdOfPrey)] = "Images/hawk.png";
            imageMap[typeof(FruitTree)] = "Images/tree.png";
            imageMap[typeof(Human)] = "Images/human.png";
            imageMap[typeof(Cetacean)] = "Images/whale.png";
            imageMap[typeof(Primate)] = "Images/primate.png";
            imageMap[typeof(Grass)] = "Images/grass.png";
        }

        private void TypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DynamicFieldsPanel.Children.Clear();
            fieldInputs.Clear();

            var type = (Type)TypeComboBox.SelectedItem;
            if (type == null) return;

            currentConstructor = type.GetConstructors()
                .OrderByDescending(c => c.GetParameters().Length)
                .FirstOrDefault();

            if (currentConstructor == null)
            {
                MessageBox.Show("Не найден подходящий конструктор.");
                return;
            }

            foreach (var param in currentConstructor.GetParameters())
            {
                string translatedName = TranslateParamName(param.Name ?? string.Empty);

                var label = new TextBlock { Text = translatedName, Margin = new Thickness(0, 5, 0, 2) };
                DynamicFieldsPanel.Children.Add(label);

                FrameworkElement input;
                if (param.ParameterType == typeof(bool))
                {
                    input = new CheckBox();
                }
                else
                {
                    input = new TextBox();
                }

                DynamicFieldsPanel.Children.Add(input);
                fieldInputs[param.Name!] = input;
            }
        }

        private void CreateAnimal_Click(object sender, RoutedEventArgs e)
        {
            if (currentConstructor == null) return;

            try
            {
                var parameters = currentConstructor.GetParameters();
                object[] args = new object[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    if (fieldInputs[parameters[i].Name] is CheckBox checkbox)
                    {
                        args[i] = checkbox.IsChecked == true;
                    }
                    else if (fieldInputs[parameters[i].Name] is TextBox textBox)
                    {
                        string input = textBox.Text;
                        args[i] = Convert.ChangeType(input, parameters[i].ParameterType);
                    }
                }

                var animal = (livingOrgs)currentConstructor.Invoke(args);
                animals.Add(animal);
                AnimalListBox.Items.Add(animal);

                // Очистка формы после создания
                TypeComboBox.SelectedIndex = -1;
                DynamicFieldsPanel.Children.Clear();
                fieldInputs.Clear();
                currentConstructor = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при создании: " + ex.Message);
            }
        }

        private void ClearForm()
        {
            TypeComboBox.SelectedItem = null;
            DynamicFieldsPanel.Children.Clear();
            fieldInputs.Clear();
        }

        private void DeleteAnimal_Click(object sender, RoutedEventArgs e)
        {
            if (AnimalListBox.SelectedItem is livingOrgs animal)
            {
                animals.Remove(animal);
                AnimalListBox.Items.Remove(animal);
            }
        }

        private void ShowAll_Click(object sender, RoutedEventArgs e)
        {
            AnimalListBox.Items.Clear();
            foreach (var animal in animals)
                AnimalListBox.Items.Add(animal);
        }

        private void AnimalListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AnimalListBox.SelectedItem is livingOrgs animal)
            {
                var infoWindow = new AnimalDetailsWindow(animal, imageMap);
                infoWindow.Owner = this;
                infoWindow.ShowDialog();
            }
        }

        private string TranslateParamName(string name)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["name"] = "Имя",
                ["age"] = "Возраст",
                ["weight"] = "Вес",
                ["isPredator"] = "Хищник",
                ["canFly"] = "Умеет летать",
                ["species"] = "Вид",
                ["color"] = "Цвет",
                ["height"] = "Рост",
                ["isEndangered"] = "Находится под угрозой",
                ["hasFur"] = "Имеет шерсть"
                // Добавь сюда другие параметры по мере необходимости
            };

            return dict.TryGetValue(name, out var translated) ? translated : name;
        }
    }
}
