using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MQTTnet.App.Controls.QualityOfServiceLevel;

public class QualityOfServiceLevelSelectorView : UserControl
{
    public QualityOfServiceLevelSelectorView()
    {
        InitializeComponent();
    }

    void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}