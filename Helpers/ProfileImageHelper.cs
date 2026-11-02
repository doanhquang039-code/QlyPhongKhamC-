using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

namespace QlyPhongKham.Helpers
{
    public static class ProfileImageHelper
    {
        private const int MaxFileSizeBytes = 10 * 1024 * 1024;
        private const int MaxImageDimension = 640;

        public static byte[] EncodeForStorage(string filePath)
        {
            var file = new FileInfo(filePath);
            if (!file.Exists)
                throw new FileNotFoundException("Không tìm thấy tệp ảnh.", filePath);
            if (file.Length > MaxFileSizeBytes)
                throw new InvalidDataException("Ảnh phải có dung lượng không quá 10 MB.");

            using var source = Image.FromFile(filePath);
            double scale = Math.Min(1d, (double)MaxImageDimension / Math.Max(source.Width, source.Height));
            int width = Math.Max(1, (int)Math.Round(source.Width * scale));
            int height = Math.Max(1, (int)Math.Round(source.Height * scale));
            using var resized = new Bitmap(width, height);
            using (var graphics = Graphics.FromImage(resized))
            {
                graphics.Clear(Color.White);
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                graphics.DrawImage(source, 0, 0, width, height);
            }

            var jpegCodec = ImageCodecInfo.GetImageEncoders()
                .FirstOrDefault(codec => codec.FormatID == ImageFormat.Jpeg.Guid)
                ?? throw new InvalidOperationException("Không tìm thấy bộ mã hóa ảnh JPEG.");
            using var parameters = new EncoderParameters(1);
            parameters.Param[0] = new EncoderParameter(Encoder.Quality, 82L);
            using var output = new MemoryStream();
            resized.Save(output, jpegCodec, parameters);
            return output.ToArray();
        }

        public static Image? CreatePreview(byte[]? imageBytes)
        {
            if (imageBytes is not { Length: > 0 })
                return null;

            using var stream = new MemoryStream(imageBytes, writable: false);
            using var source = Image.FromStream(stream);
            return new Bitmap(source);
        }

        public static Bitmap CreateAvatar(byte[]? imageBytes, string displayName, int size = 44)
        {
            if (size <= 0)
                throw new ArgumentOutOfRangeException(nameof(size), "Avatar size must be positive.");

            var avatar = new Bitmap(size, size);
            using var graphics = Graphics.FromImage(avatar);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);
            using var clip = new GraphicsPath();
            clip.AddEllipse(0, 0, size - 1, size - 1);
            graphics.SetClip(clip);

            using (var background = new SolidBrush(Color.FromArgb(0, 145, 134)))
                graphics.FillEllipse(background, 0, 0, size, size);

            using var sourceStream = imageBytes is { Length: > 0 }
                ? new MemoryStream(imageBytes, writable: false)
                : null;
            using var source = sourceStream is null ? null : Image.FromStream(sourceStream);
            if (source is not null)
            {
                float scale = Math.Max((float)size / source.Width, (float)size / source.Height);
                float width = source.Width * scale;
                float height = source.Height * scale;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(source, (size - width) / 2, (size - height) / 2, width, height);
            }
            else
            {
                string initials = string.IsNullOrWhiteSpace(displayName)
                    ? "?"
                    : displayName.Trim().Substring(0, 1).ToUpperInvariant();
                using var font = new Font("Segoe UI", Math.Max(9, size * 0.38f), FontStyle.Bold);
                using var foreground = new SolidBrush(Color.White);
                var bounds = new RectangleF(0, 0, size, size);
                using var format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                graphics.DrawString(initials, font, foreground, bounds, format);
            }

            return avatar;
        }
    }
}
