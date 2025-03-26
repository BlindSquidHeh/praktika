using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Практика_по_архиву.DB_;

namespace Практика_по_архиву
{
    public class ProductViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ArticleNumber { get; set; }
        public string ProductTypeTitle { get; set; }
        public string ImagePath { get; set; }
        public string MaterialList { get; set; }
        public decimal CalculatedCost { get; set; }
        public string BackgroundColor { get; set; }
        public int? ProductionWorkshopNumber { get; set; } 
    }

   

    public partial class MainWindow : Window
    {
        private ПрактикаEntities dbContext;
        private List<ProductViewModel> allProducts;
        private List<ProductViewModel> displayedProducts;
        private const int PageSize = 20;
        private int currentPage = 1;
        private int totalPages;
        private const string SearchPlaceholder = "ВВЕДИТЕ ДЛЯ ПОИСКА";
        private ProductEditWindow _editWindow;

        public MainWindow()
        {
            InitializeComponent();
            try
            {
                dbContext = new ПрактикаEntities();
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при инициализации: {ex.Message}");
            }
        }

        private void LoadData()
        {
            try
            {
                allProducts = new List<ProductViewModel>();

                var productTypes = dbContext.ProductType.ToList();
                FilterComboBox.Items.Add("Все типы");
                foreach (var type in productTypes)
                {
                    FilterComboBox.Items.Add(type.Title);
                }
                FilterComboBox.SelectedIndex = 0;

                var products = dbContext.Product.ToList();
                if (products == null || !products.Any())
                {
                    MessageBox.Show("В базе данных нет продуктов.");
                    return;
                }

                var lastMonth = DateTime.Now.AddMonths(-1);

                foreach (var product in products)
                {
                    decimal cost = 0;
                    var productMaterials = dbContext.ProductMaterial
                        .Where(pm => pm.ProductID == product.ID)
                        .ToList();
                    foreach (var pm in productMaterials)
                    {
                        var material = dbContext.Material.Find(pm.MaterialID);
                        if (material != null)
                        {
                            cost += material.Cost * (decimal)(pm.Count ?? 0);
                        }
                    }

                    string materialList = "Материалы: " + string.Join(", ", productMaterials
                        .Select(pm => dbContext.Material.Find(pm.MaterialID)?.Title)
                        .Where(m => m != null));

                    bool soldLastMonth = dbContext.ProductSale
                        .Any(ps => ps.ProductID == product.ID && ps.SaleDate >= lastMonth);
                    string imagePath = "";
                    if (product.Image != null)
                    {
                        imagePath = product.Image;
                    }
                    else {
                        imagePath = "\\images__\\picture.png";
                    }

                        allProducts.Add(new ProductViewModel
                        {
                            Id = product.ID,
                            Title = product.Title,
                            Description = product.Description,
                            ArticleNumber = $"Артикул: {product.ArticleNumber}",
                            ProductTypeTitle = product.ProductType?.Title ?? "Не указан",
                            ImagePath = imagePath,
                            MaterialList = materialList,
                            CalculatedCost = cost,
                            BackgroundColor = soldLastMonth ? "White" : "LightPink",
                            ProductionWorkshopNumber = product.ProductionWorkshopNumber
                        });
                }

                ApplyFiltersAndSort();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}");
            }
        }

        private void ApplyFiltersAndSort()
        {
            try
            {
                if (allProducts == null)
                {
                    MessageBox.Show("Список продуктов не инициализирован.");
                    return;
                }

                var filteredProducts = allProducts.AsQueryable();

                string searchText = SearchTextBox.Text == SearchPlaceholder ? "" : SearchTextBox.Text.ToLower();
                if (!string.IsNullOrEmpty(searchText))
                {
                    filteredProducts = filteredProducts.Where(p =>
                        p.Title.ToLower().Contains(searchText) ||
                        (p.Description != null && p.Description.ToLower().Contains(searchText)));
                }

                string selectedFilter = FilterComboBox.SelectedItem?.ToString();
                if (selectedFilter != "Все типы")
                {
                    filteredProducts = filteredProducts.Where(p => p.ProductTypeTitle == selectedFilter);
                }

                string selectedSort = SortComboBox.SelectedItem?.ToString();
                switch (selectedSort)
                {
                    case "По наименованию (возр.)":
                        filteredProducts = filteredProducts.OrderBy(p => p.Title);
                        break;
                    case "По наименованию (убыв.)":
                        filteredProducts = filteredProducts.OrderByDescending(p => p.Title);
                        break;
                    case "По номеру цеха (возр.)":
                        filteredProducts = filteredProducts.OrderBy(p => p.ProductionWorkshopNumber);
                        break;
                    case "По номеру цеха (убыв.)":
                        filteredProducts = filteredProducts.OrderByDescending(p => p.ProductionWorkshopNumber);
                        break;
                    case "По стоимости (возр.)":
                        filteredProducts = filteredProducts.OrderBy(p => p.CalculatedCost);
                        break;
                    case "По стоимости (убыв.)":
                        filteredProducts = filteredProducts.OrderByDescending(p => p.CalculatedCost);
                        break;
                }

                var productList = filteredProducts.ToList();
                totalPages = (int)Math.Ceiling((double)productList.Count / PageSize);
                displayedProducts = productList.Skip((currentPage - 1) * PageSize).Take(PageSize).ToList();

                ProductListBox.ItemsSource = displayedProducts;
                UpdatePagination();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при фильтрации и сортировке: {ex.Message}");
            }
        }

        private void UpdatePagination()
        {
            PageNumbers.Items.Clear();
            for (int i = 1; i <= totalPages; i++)
            {
                PageNumbers.Items.Add(i);
            }
            PrevPageButton.IsEnabled = currentPage > 1;
            NextPageButton.IsEnabled = currentPage < totalPages;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SearchTextBox.Text == SearchPlaceholder) return;
            currentPage = 1;
            ApplyFiltersAndSort();
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox.Text == SearchPlaceholder)
            {
                SearchTextBox.Text = "";
                SearchTextBox.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                SearchTextBox.Text = SearchPlaceholder;
                SearchTextBox.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            currentPage = 1;
            ApplyFiltersAndSort();
        }

        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            currentPage = 1;
            ApplyFiltersAndSort();
        }

        private void PrevPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage > 1)
            {
                currentPage--;
                ApplyFiltersAndSort();
            }
        }

        private void NextPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage < totalPages)
            {
                currentPage++;
                ApplyFiltersAndSort();
            }
        }

        private void PageNumberButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && int.TryParse(button.Content.ToString(), out int page))
            {
                currentPage = page;
                ApplyFiltersAndSort();
            }
        }

        private void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            if (_editWindow != null)
            {
                MessageBox.Show("Уже открыто окно редактирования. Закройте его перед открытием нового.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _editWindow = new ProductEditWindow(dbContext);
            _editWindow.Closed += (s, args) => _editWindow = null;
            if (_editWindow.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void ProductListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ProductListBox.SelectedItem is ProductViewModel selectedProduct)
            {
                if (_editWindow != null)
                {
                    MessageBox.Show("Уже открыто окно редактирования. Закройте его перед открытием нового.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var product = dbContext.Product.Find(selectedProduct.Id);
                if (product != null)
                {
                    _editWindow = new ProductEditWindow(dbContext, product);
                    _editWindow.Closed += (s, args) => _editWindow = null;
                    if (_editWindow.ShowDialog() == true)
                    {
                        LoadData();
                    }
                }
            }
        }
    }
}