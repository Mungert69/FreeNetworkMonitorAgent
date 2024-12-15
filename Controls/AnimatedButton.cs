using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace NetworkMonitor.Maui.Controls
{
    public class AnimatedButton : Button
    {
        public AnimatedButton()
        {
            Clicked += async (sender, e) => 
            {
                await this.ScaleTo(0.9, 50, Easing.Linear);
                await this.ScaleTo(1, 50, Easing.Linear);
            };
        }
    }
}
