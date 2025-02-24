﻿using KeySecret.App.Library.Models;
using KeySecret.App.Library;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace KeySecret.App.Desktop
{
    public partial class MainWindow : Window
    {
        private readonly IEndpoint<AccountModel> _accountEndpoint;

        private readonly IEndpoint<CategoryModel> _categoryEndpoint;

        public static ObservableCollection<AccountModel> AccountsList { get; set; }
        public static ObservableCollection<CategoryModel> CategoryList { get; set; }

        public MainWindow(IEndpoint<AccountModel> accountEndpoint, IEndpoint<CategoryModel> categoryEndpoint)
        {
            InitializeComponent();

            _accountEndpoint = accountEndpoint;
            _categoryEndpoint = categoryEndpoint;

            AccountsList = new ObservableCollection<AccountModel>();
            CategoryList = new ObservableCollection<CategoryModel>();

            lb_Accounts.ItemsSource = AccountsList;
            lb_Categories.ItemsSource = CategoryList;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) => this.DragMove();

        private void Exit_Click(object sender, RoutedEventArgs e) => Environment.Exit(0);
        private void Quit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Add(object sender, RoutedEventArgs e)
        {
            AddDialog _dialogBox = new AddDialog();
            _dialogBox.ShowDialog();
            lb_Categories.Items.Add(CategoryList);
        }

        private void Remove(object sender, RoutedEventArgs e)
        {
            // Da nun die Datenbank Models geladen werden ist diese Logik hinfällig

            //if (Categorie_Area.SelectedItem == null || Categorie_Area.SelectedItem.Equals(Allgemein))
            //{
            //    return;
            //}
            //Categorie_Area.Items.Remove(Categorie_Area.SelectedItem);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadAccountsAsync();
            await LoadCategoriesAsync();
        }

        private void TestUpdateAccount_OnClick(object sender, RoutedEventArgs e)
        {
            //var item = new AccountModel();
            //item.Id = AccountsList[2].Id;
            //item.Name = AccountsList[2].Name + "*updated*";
            //item.WebAdress = AccountsList[2].WebAdress;
            //item.Password = AccountsList[2].Password;

            //_accountEndpoint.UpdateAsync(item);
        }

        private void NewPw_Click(object sender, RoutedEventArgs e)
        {
            AddPw _addPw = new AddPw(_accountEndpoint);
            _addPw.ShowDialog();
        }

        private async void Remove_Entry(object sender, RoutedEventArgs e)
        {
            AccountModel model = (AccountModel)lb_Accounts.SelectedItem;

            await _accountEndpoint.DeleteAsync(model.Id);
            await LoadAccountsAsync();
        }

        private void Change_Entry(object sender, RoutedEventArgs e)
        {
            ChangeEntry _change = new ChangeEntry(_accountEndpoint, (AccountModel)lb_Accounts.SelectedItem);
            _change.ShowDialog();
        }

        public async Task LoadAccountsAsync()
        {
            if (AccountsList == null)
                AccountsList = new ObservableCollection<AccountModel>();

            AccountsList.Clear();

            foreach (var item in await _accountEndpoint.GetAllAsync())
            {
                AccountsList.Add(item);
            }
        }

        public async Task LoadCategoriesAsync()
        {
            if (CategoryList == null)
                CategoryList = new ObservableCollection<CategoryModel>();

            CategoryList.Clear();

            foreach (var item in await _categoryEndpoint.GetAllAsync())
            {
                CategoryList.Add(item);
            }
        }
    }
}