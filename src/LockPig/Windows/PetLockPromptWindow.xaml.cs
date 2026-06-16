using LockPig.Localization;
using System.Windows;
using System.Windows.Input;

namespace LockPig.Windows;

public partial class PetLockPromptWindow : Window
{
    public PetLockPromptWindow(LocalizedStrings strings, int remainingPromptDisplays)
    {
        InitializeComponent();
        TitleText.Text = strings.PetTripleClickPromptTitle;
        MessageText.Text = strings.PetTripleClickPromptMessage;
        RemainingText.Text = string.Format(strings.PetTripleClickPromptRemaining, Math.Max(0, remainingPromptDisplays - 1));
        LockButton.Content = strings.PetTripleClickPromptLock;
        SkipButton.Content = strings.PetTripleClickPromptSkip;
    }

    private void LockButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void SkipButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
}
