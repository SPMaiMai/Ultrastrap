using System.Drawing;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Bloxstrap.Extensions
{
    static class BootstrapperIconEx
    {
        private static readonly Uri UltrastrapPngUri = new("pack://application:,,,/Resources/Ultrastrap.png", UriKind.Absolute);
        public static IReadOnlyCollection<BootstrapperIcon> Selections => new BootstrapperIcon[]
        {
            BootstrapperIcon.IconBloxstrap,
            BootstrapperIcon.Icon2022,
            BootstrapperIcon.Icon2019,
            BootstrapperIcon.Icon2017,
            BootstrapperIcon.IconLate2015,
            BootstrapperIcon.IconEarly2015,
            BootstrapperIcon.Icon2011,
            BootstrapperIcon.Icon2008,
            BootstrapperIcon.IconBloxstrapClassic,
            BootstrapperIcon.IconCustom
        };

        // small note on handling icon sizes
        // i'm using multisize icon packs here with sizes 16, 24, 32, 48, 64 and 128
        // use this for generating multisize packs: https://www.aconvert.com/icon/

        public static Icon GetIcon(this BootstrapperIcon icon)
        {
            const string LOG_IDENT = "BootstrapperIconEx::GetIcon";

            // load the custom icon file
            if (icon == BootstrapperIcon.IconCustom)
            {
                Icon? customIcon = null;
                string location = App.Settings.Prop.BootstrapperIconCustomLocation;

                if (String.IsNullOrEmpty(location)) 
                {
                    App.Logger.WriteLine(LOG_IDENT, "Warning: custom icon is not set.");
                }
                else
                {
                    try
                    {
                        customIcon = new Icon(location);
                    }
                    catch (Exception ex)
                    {
                        App.Logger.WriteLine(LOG_IDENT, $"Failed to load custom icon!");
                        App.Logger.WriteException(LOG_IDENT, ex);
                    }
                }

                return customIcon ?? Properties.Resources.IconBloxstrap;
            }

            return icon switch
            {
                BootstrapperIcon.IconBloxstrap => Properties.Resources.IconBloxstrap,
                BootstrapperIcon.Icon2008 => Properties.Resources.Icon2008,
                BootstrapperIcon.Icon2011 => Properties.Resources.Icon2011,
                BootstrapperIcon.IconEarly2015 => Properties.Resources.IconEarly2015,
                BootstrapperIcon.IconLate2015 => Properties.Resources.IconLate2015,
                BootstrapperIcon.Icon2017 => Properties.Resources.Icon2017,
                BootstrapperIcon.Icon2019 => Properties.Resources.Icon2019,
                BootstrapperIcon.Icon2022 => Properties.Resources.Icon2022,
                BootstrapperIcon.IconBloxstrapClassic => Properties.Resources.IconBloxstrapClassic,
                _ => Properties.Resources.IconBloxstrap
            };
        }

        /// <summary>
        /// WPF image for the selected bootstrapper icon. The default Ultrastrap mark is
        /// loaded from a 1024px PNG because System.Drawing.Icon only keeps a 32px frame.
        /// </summary>
        public static ImageSource GetImageSource(this BootstrapperIcon icon)
        {
            if (icon == BootstrapperIcon.IconBloxstrap)
                return LoadUltrastrapPng();

            return icon.GetIcon().GetImageSource();
        }

        public static Bitmap GetBitmap(this BootstrapperIcon icon, int size)
        {
            if (icon == BootstrapperIcon.IconBloxstrap)
            {
                using var source = LoadUltrastrapBitmap();
                return new Bitmap(source, new Size(size, size));
            }

            return icon.GetIcon().GetSized(size, size).ToBitmap();
        }

        private static ImageSource LoadUltrastrapPng()
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = UltrastrapPngUri;
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.DecodePixelWidth = 256;
            image.EndInit();
            image.Freeze();
            return image;
        }

        private static Bitmap LoadUltrastrapBitmap()
        {
            var stream = System.Windows.Application.GetResourceStream(UltrastrapPngUri)?.Stream
                ?? throw new InvalidOperationException("Ultrastrap.png resource is missing");

            return new Bitmap(stream);
        }
    }
}
