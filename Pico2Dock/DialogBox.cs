using System.Windows;
using Wpf.Ui.Controls;

namespace Pico2Dock
{
    class DialogBox
    {
        public static async Task<bool> Show(string title, string content, string leftButtonText, string rightButtonText)
        {
            var dialogHostContainer = App.mainWindow?.DialogHostContainer;
            var dialog = new ContentDialog
            {
                Title = new TextBlock
                {
                    Text = title,
                    FontSize = 18,
                    FontWeight = FontWeights.Regular
                },
                Content = content,
                PrimaryButtonText = leftButtonText,
                CloseButtonText = rightButtonText,
                DefaultButton = ContentDialogButton.Close,
                DialogHost = dialogHostContainer,
                Padding = new Thickness(8, 4, 8, 8),
            };

            dialogHostContainer.Visibility = Visibility.Visible;
            var result = await dialog.ShowAsync();
            dialogHostContainer.Visibility = Visibility.Collapsed;

            return result == ContentDialogResult.Primary;
        }
    }
}
