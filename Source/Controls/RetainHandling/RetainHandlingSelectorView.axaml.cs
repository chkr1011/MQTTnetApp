using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace mqttMultimeter.Controls;

public sealed class RetainHandlingSelectorView : UserControl
{
    public RetainHandlingSelectorView()
    {
        InitializeComponent();
    }

    void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}