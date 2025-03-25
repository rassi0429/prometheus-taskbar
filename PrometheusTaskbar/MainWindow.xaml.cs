﻿using System.Windows;

namespace PrometheusTaskbar;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // Hide the window instead of closing it
        e.Cancel = true;
        Hide();
        base.OnClosing(e);
    }
}
