using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Lab1_OOP;
using System.Text.Json;
using System.IO;

namespace ZooApp
{
    public partial class MainWindow : Window
    {
        // Используем ObservableCollection для автоматического обновления GUI
        private ObservableCollection<livingOrgs> animals = new();
        private Dictionary<string, FrameworkElement> fieldInputs = new();
        private ConstructorInfo? currentConstructor;
    
        public MainWindow()
        {
            InitializeComponent();
            AnimalListBox.ItemsSource = animals;
            LoadAnimalTypes();
        }

        private static readonly Dictionary<string, Type> TypeMap = new(StringComparer.OrdinalIgnoreCase);

        private void LoadAnimalTypes()
        {
            try
            {
                var types = Assembly.Load("Lab1_OOP")
                    .GetTypes()
                    .Where(t => typeof(livingOrgs).IsAssignableFrom(t) && !t.IsAbstract)
                    .ToList();

                foreach (var type in types)
                {
                    TypeMap[type.Name] = type;
                }

                TypeComboBox.ItemsSource = types;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки типов: {ex.Message}");
            }
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

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "JSON Files|*.json",
                DefaultExt = ".json",
                FileName = "animals"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Преобразуем ObservableCollection -> List
                    AnimalSerializer.SaveAnimals(animals.ToList(), dialog.FileName);
                    MessageBox.Show("Данные сохранены!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}");
                }
            }
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "JSON Files|*.json",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Преобразуем List -> ObservableCollection
                    animals = new ObservableCollection<livingOrgs>(
                        AnimalSerializer.LoadAnimals(dialog.FileName)
                    );

                    AnimalListBox.ItemsSource = animals; // Обновляем привязку
                    MessageBox.Show("Данные успешно загружены!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке: {ex.Message}");
                }
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
                animals.Add(animal); // Добавляем в коллекцию
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при создании: " + ex.Message);
            }
        }

        private void DeleteAnimal_Click(object sender, RoutedEventArgs e)
        {
            if (AnimalListBox.SelectedItem is livingOrgs selectedAnimal)
            {
                animals.Remove(selectedAnimal); // Удаляем через коллекцию
            }
        }

        private void ShowAll_Click(object sender, RoutedEventArgs e)
        {
            // Ничего не делаем — данные уже отображаются через ItemsSource
        }

        private void AnimalListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AnimalListBox.SelectedItem is livingOrgs animal)
            {
                var infoWindow = new AnimalDetailsWindow(animal);
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
                ["hasFur"] = "Имеет шерсть",
                ["wingspan"] = "Размах крыльев",
                ["prey"] = "Добыча",
                ["length"] = "Длина",
                ["occupation"] = "Работа",
                ["hadJob"] = "Есть ли работа?",
                ["canUseTools"] = "Может ли использовать предметы?",
                ["fruitType"] = "Тип фрукта",
                ["isMowed"] = "Двигается ли?"
            };

            return dict.TryGetValue(name, out var translated) ? translated : name;
        }
    }
}