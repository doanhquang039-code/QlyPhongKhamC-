using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace QlyPhongKham.Forms
{
    public sealed class FormLandingHome : Form
    {
        // ── Brand Colors ──
        private static readonly Color C_Primary     = Color.FromArgb(14, 165, 233);      // #0ea5e9
        private static readonly Color C_PrimaryDark = Color.FromArgb(2, 132, 199);       // #0284c7
        private static readonly Color C_Dark        = Color.FromArgb(15, 23, 42);        // #0f172a
        private static readonly Color C_Light       = Color.FromArgb(248, 250, 252);    // #f8fafc
        private static readonly Color C_Gray        = Color.FromArgb(100, 116, 139);     // #64748b
        private static readonly Color C_Border      = Color.FromArgb(226, 232, 240);     // #e2e8f0

        private Panel _scrollContent = null!;
        private Panel _nav = null!;
        private Action? _relayoutAction;

        public FormLandingHome()
        {
            this.Text = "MediCore Clinic - Chăm Sóc Sức Khỏe Toàn Diện";
            this.Size = new Size(1366, 850);
            this.MinimumSize = new Size(1024, 700);
            this.WindowState = FormWindowState.Maximized; // Full màn hình
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);

            BuildNavbar();
            BuildMainLandingContent();

            this.Resize += (s, e) => _relayoutAction?.Invoke();
        }

        // ════════════════════════════════════════════════════════════
        // 1. TOP NAVBAR (Căn giữa Menu hoàn hảo, nút bám góc phải)
        // ════════════════════════════════════════════════════════════
        private void BuildNavbar()
        {
            _nav = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                BackColor = Color.White
            };
            _nav.Paint += (s, e) =>
            {
                using var p = new Pen(C_Border, 1);
                e.Graphics.DrawLine(p, 0, _nav.Height - 1, _nav.Width, _nav.Height - 1);
            };

            // Logo & Brand
            var lblLogoHeart = new Label
            {
                Text = "❤",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(239, 68, 68),
                Location = new Point(32, 18),
                AutoSize = true
            };
            var lblBrand = new Label
            {
                Text = "MediCore",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = C_Primary,
                Location = new Point(66, 19),
                AutoSize = true
            };

            // Menu container (Căn giữa màn hình)
            var pnlLinks = new FlowLayoutPanel
            {
                Size = new Size(650, 40),
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent,
                WrapContents = false
            };

            string[] menuItems = { "Dịch vụ", "Bác sĩ", "Quy trình", "Đánh giá", "Hỏi đáp", "Về chúng tôi" };
            foreach (var item in menuItems)
            {
                var btnMenu = new Button
                {
                    Text = item,
                    AutoSize = true,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.Transparent,
                    ForeColor = Color.FromArgb(51, 65, 85),
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Margin = new Padding(10, 2, 10, 0),
                    Padding = new Padding(4, 2, 4, 2)
                };
                btnMenu.FlatAppearance.BorderSize = 0;
                btnMenu.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
                pnlLinks.Controls.Add(btnMenu);
            }

            // Auth buttons (Right side)
            var btnLogin = new Button
            {
                Text = "Đăng nhập",
                Size = new Size(115, 38),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(238, 242, 255),
                ForeColor = C_Primary,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 1;
            btnLogin.FlatAppearance.BorderColor = Color.FromArgb(186, 230, 253);

            var btnRegister = new Button
            {
                Text = "Đăng ký",
                Size = new Size(115, 38),
                FlatStyle = FlatStyle.Flat,
                BackColor = C_Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRegister.FlatAppearance.BorderSize = 0;

            btnLogin.Click += (s, e) =>
            {
                var loginForm = new FormLogin();
                loginForm.FormClosed += (sender, args) => this.Show();
                this.Hide();
                loginForm.Show();
            };

            btnRegister.Click += (s, e) =>
            {
                var reg = new FormDangKyBenhNhan();
                reg.ShowDialog();
            };

            void LayoutNavbar()
            {
                // Căn menu ra chính giữa chiều ngang
                pnlLinks.Location = new Point(Math.Max(220, (_nav.ClientSize.Width - pnlLinks.Width) / 2), 18);
                // Nút bám góc phải
                btnLogin.Location = new Point(_nav.ClientSize.Width - 255, 17);
                btnRegister.Location = new Point(_nav.ClientSize.Width - 130, 17);
            }
            _nav.Resize += (s, e) => LayoutNavbar();
            LayoutNavbar();

            _nav.Controls.AddRange(new Control[] { lblLogoHeart, lblBrand, pnlLinks, btnLogin, btnRegister });
            this.Controls.Add(_nav);
        }

        // ════════════════════════════════════════════════════════════
        // 2. MAIN SCROLLABLE RESPONSIVE CONTAINER
        // ════════════════════════════════════════════════════════════
        private void BuildMainLandingContent()
        {
            _scrollContent = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White
            };

            var relayoutList = new System.Collections.Generic.List<Action>();

            int currentY = 0;

            // 1. HERO
            var hero = BuildHeroSection(ref currentY, relayoutList);
            _scrollContent.Controls.Add(hero);

            // 2. SERVICES
            var services = BuildServicesSection(ref currentY, relayoutList);
            _scrollContent.Controls.Add(services);

            // 3. DOCTORS
            var doctors = BuildDoctorsSection(ref currentY, relayoutList);
            _scrollContent.Controls.Add(doctors);

            // 4. PROCESS
            var process = BuildProcessSection(ref currentY, relayoutList);
            _scrollContent.Controls.Add(process);

            // 5. STATS
            var stats = BuildStatsSection(ref currentY, relayoutList);
            _scrollContent.Controls.Add(stats);

            // 6. TESTIMONIALS
            var reviews = BuildTestimonialsSection(ref currentY, relayoutList);
            _scrollContent.Controls.Add(reviews);

            // 7. FAQ
            var faq = BuildFaqSection(ref currentY, relayoutList);
            _scrollContent.Controls.Add(faq);

            // 8. CTA
            var cta = BuildCtaSection(ref currentY, relayoutList);
            _scrollContent.Controls.Add(cta);

            // 9. FOOTER
            var footer = BuildFooterSection(ref currentY, relayoutList);
            _scrollContent.Controls.Add(footer);

            _relayoutAction = () =>
            {
                _scrollContent.SuspendLayout();
                foreach (var act in relayoutList) act.Invoke();
                _scrollContent.ResumeLayout();
            };

            this.Controls.Add(_scrollContent);
            _scrollContent.SizeChanged += (s, e) => _relayoutAction?.Invoke();
        }

        // ════════════════════════════════════════════════════════════
        // SECTION 1: HERO BANNER (Vẽ trực tiếp chống lỗi màu trắng/đè chữ)
        // ════════════════════════════════════════════════════════════
        private Panel BuildHeroSection(ref int topY, System.Collections.Generic.List<Action> relayoutList)
        {
            int h = 480;
            var hero = new Panel
            {
                Location = new Point(0, topY),
                Size = new Size(_scrollContent.ClientSize.Width, h),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            topY += h;

            // 2 Action buttons (Clickable Controls)
            var btnBookNow = new Button
            {
                Text = "📅  ĐẶT LỊCH NGAY",
                Size = new Size(210, 48),
                BackColor = Color.White,
                ForeColor = C_PrimaryDark,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnBookNow.FlatAppearance.BorderSize = 0;
            btnBookNow.Click += (s, e) =>
            {
                var login = new FormLogin();
                login.Show();
                this.Hide();
            };

            var btnLearnMore = new Button
            {
                Text = "Tìm hiểu thêm ➔",
                Size = new Size(180, 48),
                BackColor = Color.FromArgb(40, 255, 255, 255),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnLearnMore.FlatAppearance.BorderColor = Color.FromArgb(180, 255, 255, 255);

            // Right Hero Card (Floating Card trắng sạch đẹp)
            var heroCard = new Panel
            {
                Size = new Size(360, 260),
                BackColor = Color.White,
                Padding = new Padding(24)
            };
            heroCard.Paint += (s, e) =>
            {
                using var p = new Pen(C_Border, 1.5f);
                e.Graphics.DrawRectangle(p, 0, 0, heroCard.Width - 1, heroCard.Height - 1);
            };

            var heroIcon = new Label
            {
                Text = "🏥",
                Font = new Font("Segoe UI", 36),
                Location = new Point(24, 18),
                AutoSize = true
            };
            var statTitle = new Label
            {
                Text = "10,000+",
                Location = new Point(24, 95),
                AutoSize = true,
                Font = new Font("Segoe UI", 26, FontStyle.Bold),
                ForeColor = C_Dark
            };
            var statDesc = new Label
            {
                Text = "Bệnh nhân hài lòng tin chọn MediCore\nChứng nhận an toàn chuẩn Bộ Y Tế",
                Location = new Point(24, 155),
                Size = new Size(310, 50),
                Font = new Font("Segoe UI", 10.5F),
                ForeColor = C_Gray
            };
            heroCard.Controls.AddRange(new Control[] { heroIcon, statTitle, statDesc });

            // OnPaint: Vẽ toàn bộ text và background Gradient mượt mà, KHÔNG BỊ TRẮNG NỀN
            hero.Paint += (s, e) =>
            {
                if (hero.ClientSize.Width <= 0 || hero.ClientSize.Height <= 0)
                    return;

                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                // Gradient background
                using var br = new LinearGradientBrush(hero.ClientRectangle,
                    Color.FromArgb(14, 165, 233), Color.FromArgb(59, 130, 246), 45f);
                g.FillRectangle(br, hero.ClientRectangle);

                // Subtle circular decors
                using var decor = new SolidBrush(Color.FromArgb(20, 255, 255, 255));
                g.FillEllipse(decor, hero.Width - 360, -100, 500, 500);
                g.FillEllipse(decor, -60, 280, 280, 280);

                int startX = Math.Max(50, (int)(hero.Width * 0.06));

                // Badge: ★ Phòng khám tốt nhất 2026
                var badgeRect = new Rectangle(startX, 45, 240, 32);
                using (var badgePath = GetRoundedRect(badgeRect, 16))
                using (var badgeBrush = new SolidBrush(Color.White))
                    g.FillPath(badgeBrush, badgePath);

                using (var fontBadge = new Font("Segoe UI", 9.5F, FontStyle.Bold))
                using (var brushBadge = new SolidBrush(C_PrimaryDark))
                {
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString("★  Phòng khám tốt nhất 2026", fontBadge, brushBadge, badgeRect, sf);
                }

                // Tiêu đề lớn
                using (var fontTitle = new Font("Segoe UI", 32F, FontStyle.Bold))
                using (var brushWhite = new SolidBrush(Color.White))
                {
                    g.DrawString("Chăm Sóc Sức Khỏe\nToàn Diện Cho Bạn", fontTitle, brushWhite, startX - 2, 95);
                }

                // Phụ đề
                using (var fontSub = new Font("Segoe UI", 11.5F))
                using (var brushSub = new SolidBrush(Color.FromArgb(240, 249, 255)))
                {
                    string subText = "Hệ thống quản lý phòng khám hiện đại với đội ngũ y bác sĩ hàng đầu.\nĐặt lịch khám dễ dàng và trải nghiệm dịch vụ y tế chuẩn quốc tế ngay hôm nay.";
                    g.DrawString(subText, fontSub, brushSub, new RectangleF(startX, 230, 640, 60));
                }
            };

            relayoutList.Add(() =>
            {
                hero.Width = _scrollContent.ClientSize.Width;
                int startX = Math.Max(50, (int)(hero.Width * 0.06));
                btnBookNow.Location = new Point(startX, 315);
                btnLearnMore.Location = new Point(startX + 225, 315);
                heroCard.Location = new Point(Math.Max(520, hero.ClientSize.Width - heroCard.Width - startX), 100);
                hero.Invalidate();
            });

            hero.Controls.AddRange(new Control[] { btnBookNow, btnLearnMore, heroCard });
            return hero;
        }

        // ════════════════════════════════════════════════════════════
        // SECTION 2: SERVICES (Chuyên khoa & Dịch vụ)
        // ════════════════════════════════════════════════════════════
        private Panel BuildServicesSection(ref int topY, System.Collections.Generic.List<Action> relayoutList)
        {
            int h = 480;
            var pnl = new Panel
            {
                Location = new Point(0, topY),
                Size = new Size(_scrollContent.ClientSize.Width, h),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White
            };
            topY += h;

            var title = new Label
            {
                Text = "Chuyên Khoa & Dịch Vụ",
                Location = new Point(0, 30),
                Size = new Size(pnl.Width, 38),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = C_Dark
            };
            var sub = new Label
            {
                Text = "Chúng tôi cung cấp đa dạng các chuyên khoa và dịch vụ khám chữa bệnh chất lượng cao.",
                Location = new Point(0, 75),
                Size = new Size(pnl.Width, 25),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 11F),
                ForeColor = C_Gray
            };
            pnl.Controls.AddRange(new Control[] { title, sub });

            string[,] services = {
                { "🩺", "Khoa Nội", "Chuyên khám và điều trị các bệnh lý nội khoa mãn tính & tổng quát." },
                { "🔪", "Khoa Ngoại", "Chuyên phẫu thuật tiểu phẫu và điều trị các bệnh lý ngoại khoa." },
                { "👶", "Nhi khoa", "Chuyên khám và chăm sóc sức khỏe toàn diện cho trẻ em." },
                { "🤰", "Sản phụ khoa", "Chuyên khám và điều trị phụ khoa, chăm sóc sức khỏe thai sản." },
                { "🦷", "Nha khoa", "Chăm sóc, tẩy trắng và điều trị chuyên sâu răng hàm mặt." },
                { "✨", "Da liễu", "Điều trị chuyên sâu các bệnh về da và phục hồi thẩm mỹ." }
            };

            var cards = new Panel[6];
            for (int i = 0; i < 6; i++)
            {
                var card = new Panel
                {
                    BackColor = Color.White,
                    Padding = new Padding(16)
                };
                card.Paint += (s, e) =>
                {
                    using var p = new Pen(C_Border, 1.5f);
                    e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
                };

                var icon = new Label
                {
                    Text = services[i, 0],
                    Font = new Font("Segoe UI", 22),
                    Location = new Point(16, 16),
                    Size = new Size(48, 48),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                var name = new Label
                {
                    Text = services[i, 1],
                    Font = new Font("Segoe UI", 12, FontStyle.Bold),
                    ForeColor = C_Dark,
                    Location = new Point(80, 18),
                    Size = new Size(200, 26),
                    AutoEllipsis = true
                };
                var desc = new Label
                {
                    Text = services[i, 2],
                    Font = new Font("Segoe UI", 9.5F),
                    ForeColor = C_Gray,
                    Location = new Point(80, 50),
                    Size = new Size(200, 60)
                };
                
                card.Controls.AddRange(new Control[] { icon, name, desc });
                card.Resize += (s, e) =>
                {
                    int textWidth = Math.Max(0, card.ClientSize.Width - 112);
                    name.Width = textWidth;
                    desc.Width = textWidth;
                };

                cards[i] = card;
                pnl.Controls.Add(card);
            }

            relayoutList.Add(() =>
            {
                pnl.Width = _scrollContent.ClientSize.Width;
                int totalW = pnl.ClientSize.Width;
                int cols = totalW >= 1100 ? 3 : 2;
                pnl.Height = cols == 3 ? 480 : 620;

                int gapX = 24;
                int gapY = 20;
                int sideMargin = Math.Max(40, (int)(totalW * 0.06));
                int cardW = (totalW - sideMargin * 2 - gapX * (cols - 1)) / cols;
                int cardH = 140;

                for (int i = 0; i < 6; i++)
                {
                    int r = i / cols;
                    int c = i % cols;
                    cards[i].Location = new Point(sideMargin + c * (cardW + gapX), 130 + r * (cardH + gapY));
                    cards[i].Size = new Size(cardW, cardH);
                }
            });

            return pnl;
        }

        // ════════════════════════════════════════════════════════════
        // SECTION 3: DOCTORS (Đội ngũ bác sĩ)
        // ════════════════════════════════════════════════════════════
        private Panel BuildDoctorsSection(ref int topY, System.Collections.Generic.List<Action> relayoutList)
        {
            int h = 500;
            var pnl = new Panel
            {
                Location = new Point(0, topY),
                Size = new Size(_scrollContent.ClientSize.Width, h),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = C_Light
            };
            topY += h;

            var title = new Label
            {
                Text = "Đội Ngũ Bác Sĩ Nổi Bật",
                Location = new Point(0, 30),
                Size = new Size(pnl.Width, 38),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = C_Dark
            };
            var sub = new Label
            {
                Text = "Quy tụ các bác sĩ giỏi, nhiều năm kinh nghiệm từ các bệnh viện lớn, tận tâm vì sức khỏe người bệnh.",
                Location = new Point(0, 75),
                Size = new Size(pnl.Width, 25),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 11F),
                ForeColor = C_Gray
            };
            pnl.Controls.AddRange(new Control[] { title, sub });

            string[,] doctors = {
                { "BS.CKII. Nguyễn Minh Khoa", "Khoa Nội", "15 năm kinh nghiệm BV Chợ Rẫy" },
                { "ThS.BS. Trần Hữu Lan", "Khoa Ngoại", "Chuyên gia phẫu thuật nội soi" },
                { "BS.CKI. Lê Quốc Minh", "Nhi khoa", "Bác sĩ tận tâm BV Nhi Đồng 1" },
                { "BS. Phạm Hoàng Phúc", "Sản phụ khoa", "Tư vấn & chăm sóc tiền sản chuyên sâu" }
            };

            var docCards = new Panel[4];
            for (int i = 0; i < 4; i++)
            {
                var card = new Panel { BackColor = Color.White };
                card.Paint += (s, e) =>
                {
                    using var p = new Pen(C_Border, 1);
                    e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
                };

                var avatar = new Label
                {
                    Text = "👨‍⚕️",
                    Font = new Font("Segoe UI", 55),
                    Location = new Point(0, 15),
                    Size = new Size(240, 110),
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.FromArgb(241, 245, 249)
                };
                var docName = new Label
                {
                    Text = doctors[i, 0],
                    Font = new Font("Segoe UI", 11, FontStyle.Bold),
                    ForeColor = C_Dark,
                    Location = new Point(10, 140),
                    Size = new Size(220, 24),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                var docSpec = new Label
                {
                    Text = doctors[i, 1],
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    ForeColor = C_Primary,
                    Location = new Point(10, 168),
                    Size = new Size(220, 22),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                var docExp = new Label
                {
                    Text = doctors[i, 2],
                    Font = new Font("Segoe UI", 9F),
                    ForeColor = C_Gray,
                    Location = new Point(10, 196),
                    Size = new Size(220, 38),
                    TextAlign = ContentAlignment.MiddleCenter
                };

                var btnDatLich = new Button
                {
                    Text = "Đặt lịch khám",
                    Location = new Point(35, 245),
                    Size = new Size(170, 36),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(240, 249, 255),
                    ForeColor = C_Primary,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnDatLich.FlatAppearance.BorderColor = Color.FromArgb(186, 230, 253);
                btnDatLich.Click += (s, e) =>
                {
                    var login = new FormLogin();
                    login.Show();
                    this.Hide();
                };

                card.Resize += (s, e) =>
                {
                    avatar.Width = card.ClientSize.Width;
                    docName.Width = card.ClientSize.Width - 20;
                    docSpec.Width = card.ClientSize.Width - 20;
                    docExp.Width = card.ClientSize.Width - 20;
                    btnDatLich.Size = new Size(card.ClientSize.Width - 60, 36);
                    btnDatLich.Location = new Point(30, 245);
                };

                card.Controls.AddRange(new Control[] { avatar, docName, docSpec, docExp, btnDatLich });
                docCards[i] = card;
                pnl.Controls.Add(card);
            }

            relayoutList.Add(() =>
            {
                pnl.Width = _scrollContent.ClientSize.Width;
                int totalW = pnl.ClientSize.Width;
                int sideMargin = Math.Max(40, (int)(totalW * 0.05));
                int gap = 20;
                int cardW = (totalW - sideMargin * 2 - gap * 3) / 4;
                int cardH = 300;

                for (int i = 0; i < 4; i++)
                {
                    docCards[i].Location = new Point(sideMargin + i * (cardW + gap), 125);
                    docCards[i].Size = new Size(cardW, cardH);
                }
            });

            return pnl;
        }

        // ════════════════════════════════════════════════════════════
        // SECTION 4: PROCESS (Quy trình khám bệnh)
        // ════════════════════════════════════════════════════════════
        private Panel BuildProcessSection(ref int topY, System.Collections.Generic.List<Action> relayoutList)
        {
            int h = 340;
            var pnl = new Panel
            {
                Location = new Point(0, topY),
                Size = new Size(_scrollContent.ClientSize.Width, h),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White
            };
            topY += h;

            var title = new Label
            {
                Text = "Quy Trình Khám Bệnh Đơn Giản",
                Location = new Point(0, 25),
                Size = new Size(pnl.Width, 38),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = C_Dark
            };
            var sub = new Label
            {
                Text = "Quy trình khám chữa bệnh được kết nối xuyên suốt từ tiếp nhận đến thanh toán.",
                Location = new Point(0, 68),
                Size = new Size(pnl.Width, 25),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 11F),
                ForeColor = C_Gray
            };
            pnl.Controls.AddRange(new Control[] { title, sub });

            string[,] steps = {
                { "1", "Tiếp Nhận & Đặt Lịch", "Lễ tân tiếp nhận hoặc bệnh nhân đặt lịch hẹn trực tuyến." },
                { "2", "Khám Lâm Sàng", "Bác sĩ kiểm tra sinh hiệu, triệu chứng và đưa ra chẩn đoán." },
                { "3", "Chỉ Định Cận Lâm Sàng", "Xét nghiệm máu, siêu âm, chụp X-quang nếu cần thiết." },
                { "4", "Kê Đơn Thuốc", "Bác sĩ xuất toa thuốc điện tử kèm hướng dẫn liều dùng." },
                { "5", "Thanh Toán & Ra Về", "Thu ngân xuất hóa đơn viện phí, nhận thuốc nhanh chóng." }
            };

            var stepCards = new Panel[5];
            for (int i = 0; i < 5; i++)
            {
                var card = new Panel
                {
                    BackColor = C_Light,
                    Padding = new Padding(10)
                };

                var num = new Label
                {
                    Text = steps[i, 0],
                    Size = new Size(46, 46),
                    BackColor = C_Primary,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 14, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                var stepTitle = new Label
                {
                    Text = steps[i, 1],
                    Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                    ForeColor = C_Dark,
                    Location = new Point(5, 65),
                    Size = new Size(180, 24),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                var stepDesc = new Label
                {
                    Text = steps[i, 2],
                    Font = new Font("Segoe UI", 8.8F),
                    ForeColor = C_Gray,
                    Location = new Point(5, 95),
                    Size = new Size(180, 65),
                    TextAlign = ContentAlignment.TopCenter
                };

                card.Resize += (s, e) =>
                {
                    num.Location = new Point((card.ClientSize.Width - num.Width) / 2, 10);
                    stepTitle.Width = card.ClientSize.Width - 10;
                    stepDesc.Width = card.ClientSize.Width - 10;
                };

                card.Controls.AddRange(new Control[] { num, stepTitle, stepDesc });
                stepCards[i] = card;
                pnl.Controls.Add(card);
            }

            relayoutList.Add(() =>
            {
                pnl.Width = _scrollContent.ClientSize.Width;
                int totalW = pnl.ClientSize.Width;
                int sideMargin = Math.Max(40, (int)(totalW * 0.05));
                int gap = 16;
                int stepW = (totalW - sideMargin * 2 - gap * 4) / 5;
                int stepH = 175;

                for (int i = 0; i < 5; i++)
                {
                    stepCards[i].Location = new Point(sideMargin + i * (stepW + gap), 120);
                    stepCards[i].Size = new Size(stepW, stepH);
                }
            });

            return pnl;
        }

        // ════════════════════════════════════════════════════════════
        // SECTION 5: STATS BANNER
        // ════════════════════════════════════════════════════════════
        private Panel BuildStatsSection(ref int topY, System.Collections.Generic.List<Action> relayoutList)
        {
            int h = 160;
            var pnl = new Panel
            {
                Location = new Point(0, topY),
                Size = new Size(_scrollContent.ClientSize.Width, h),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(241, 245, 249)
            };
            topY += h;

            string[,] stats = {
                { "15+", "NĂM KINH NGHIỆM" },
                { "50+", "BÁC SĨ CHUYÊN KHOA" },
                { "12", "PHÒNG BAN Y TẾ" },
                { "99%", "KHÁCH HÀNG HÀI LÒNG" }
            };

            var statBoxes = new Panel[4];
            for (int i = 0; i < 4; i++)
            {
                var box = new Panel { BackColor = Color.Transparent };
                var num = new Label
                {
                    Text = stats[i, 0],
                    Font = new Font("Segoe UI", 36, FontStyle.Bold),
                    ForeColor = C_Primary,
                    Location = new Point(0, 0),
                    Size = new Size(200, 60),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                var lbl = new Label
                {
                    Text = stats[i, 1],
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    ForeColor = C_Gray,
                    Location = new Point(0, 65),
                    Size = new Size(200, 25),
                    TextAlign = ContentAlignment.MiddleCenter
                };

                box.Resize += (s, e) =>
                {
                    num.Width = box.ClientSize.Width;
                    lbl.Width = box.ClientSize.Width;
                };

                box.Controls.AddRange(new Control[] { num, lbl });
                statBoxes[i] = box;
                pnl.Controls.Add(box);
            }

            relayoutList.Add(() =>
            {
                pnl.Width = _scrollContent.ClientSize.Width;
                int totalW = pnl.ClientSize.Width;
                int statW = totalW / 4;

                for (int i = 0; i < 4; i++)
                {
                    statBoxes[i].Location = new Point(i * statW, 25);
                    statBoxes[i].Size = new Size(statW, 110);
                }
            });

            return pnl;
        }

        // ════════════════════════════════════════════════════════════
        // SECTION 6: TESTIMONIALS (Đánh giá)
        // ════════════════════════════════════════════════════════════
        private Panel BuildTestimonialsSection(ref int topY, System.Collections.Generic.List<Action> relayoutList)
        {
            int h = 350;
            var pnl = new Panel
            {
                Location = new Point(0, topY),
                Size = new Size(_scrollContent.ClientSize.Width, h),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White
            };
            topY += h;

            var title = new Label
            {
                Text = "Khách Hàng Nói Gì Về Chúng Tôi",
                Location = new Point(0, 25),
                Size = new Size(pnl.Width, 38),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = C_Dark
            };
            pnl.Controls.Add(title);

            string[,] reviews = {
                { "Trần Thế Anh", "Khoa Nội", "★★★★★", "\"Dịch vụ rất tuyệt vời! Đặt lịch online khi đến được vào khám luôn không cần xếp hàng lấy số.\"" },
                { "Hoàng My", "Nha khoa", "★★★★★", "\"Bác sĩ tư vấn rất tận tâm, phòng khám sạch sẽ chuẩn 5 sao, xem lại hồ sơ trực tuyến rất tiện lợi.\"" },
                { "Phạm Phương Linh", "Nhi khoa", "★★★★★", "\"Các bác sĩ và cô điều dưỡng nhẹ nhàng, thân thiện, các bé không bị sợ như khi đến bệnh viện lớn.\"" }
            };

            var revCards = new Panel[3];
            for (int i = 0; i < 3; i++)
            {
                var card = new Panel
                {
                    BackColor = C_Light,
                    Padding = new Padding(16)
                };
                card.Paint += (s, e) =>
                {
                    using var p = new Pen(C_Border, 1);
                    e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
                };

                var stars = new Label { Text = reviews[i, 2], ForeColor = Color.FromArgb(245, 158, 11), Font = new Font("Segoe UI", 12, FontStyle.Bold), Location = new Point(16, 16), AutoSize = true };
                var text = new Label { Text = reviews[i, 3], Font = new Font("Segoe UI", 10F, FontStyle.Italic), ForeColor = Color.FromArgb(51, 65, 85), Location = new Point(16, 48), AutoSize = false };
                var author = new Label { Text = $"{reviews[i, 0]}  •  {reviews[i, 1]}", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = C_PrimaryDark, Location = new Point(16, 145), AutoSize = true };

                card.Resize += (s, e) => text.Size = new Size(card.ClientSize.Width - 32, 85);

                card.Controls.AddRange(new Control[] { stars, text, author });
                revCards[i] = card;
                pnl.Controls.Add(card);
            }

            relayoutList.Add(() =>
            {
                pnl.Width = _scrollContent.ClientSize.Width;
                int totalW = pnl.ClientSize.Width;
                int sideMargin = Math.Max(40, (int)(totalW * 0.06));
                int gap = 24;
                int revW = (totalW - sideMargin * 2 - gap * 2) / 3;
                int revH = 205;

                for (int i = 0; i < 3; i++)
                {
                    revCards[i].Location = new Point(sideMargin + i * (revW + gap), 85);
                    revCards[i].Size = new Size(revW, revH);
                }
            });

            return pnl;
        }

        // ════════════════════════════════════════════════════════════
        // SECTION 7: FAQ (Hỏi đáp)
        // ════════════════════════════════════════════════════════════
        private Panel BuildFaqSection(ref int topY, System.Collections.Generic.List<Action> relayoutList)
        {
            int h = 330;
            var pnl = new Panel
            {
                Location = new Point(0, topY),
                Size = new Size(_scrollContent.ClientSize.Width, h),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = C_Light
            };
            topY += h;

            var title = new Label
            {
                Text = "Câu Hỏi Thường Gặp (FAQ)",
                Location = new Point(0, 20),
                Size = new Size(pnl.Width, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = C_Dark
            };
            pnl.Controls.Add(title);

            string[,] faqs = {
                { "1. Tôi có thể đặt lịch khám trước bao nhiêu ngày?", "Bạn có thể đặt trước tối đa 30 ngày. Khuyến khích đặt trước ít nhất 1 ngày để phòng khám chuẩn bị tốt nhất." },
                { "2. Phòng khám có áp dụng Bảo hiểm Y tế không?", "MediCore chấp nhận BHYT nhà nước và liên kết bảo lãnh trực tiếp với hơn 20 hãng bảo hiểm lớn." },
                { "3. Làm sao để xem kết quả xét nghiệm và toa thuốc?", "Sau khi khám, chẩn đoán và toa thuốc được đồng bộ ngay trên Cổng bệnh nhân để tra cứu mọi lúc." },
                { "4. Lịch làm việc của phòng khám như thế nào?", "Phòng khám làm việc từ 7:00 đến 20:00 tất cả các ngày trong tuần (kể cả Thứ 7 và Chủ Nhật)." }
            };

            var faqBoxes = new Panel[4];
            for (int i = 0; i < 4; i++)
            {
                var item = new Panel
                {
                    BackColor = Color.White,
                    Padding = new Padding(12)
                };
                item.Paint += (s, e) =>
                {
                    using var p = new Pen(C_Border, 1);
                    e.Graphics.DrawRectangle(p, 0, 0, item.Width - 1, item.Height - 1);
                };

                var q = new Label { Text = faqs[i, 0], Font = new Font("Segoe UI", 10.5F, FontStyle.Bold), ForeColor = C_PrimaryDark, Location = new Point(14, 12), AutoSize = false };
                var a = new Label { Text = faqs[i, 1], Font = new Font("Segoe UI", 9.5F), ForeColor = C_Gray, Location = new Point(14, 38), AutoSize = false };
                
                item.Resize += (s, e) =>
                {
                    q.Size = new Size(item.ClientSize.Width - 28, 24);
                    a.Size = new Size(item.ClientSize.Width - 28, 48);
                };

                item.Controls.AddRange(new Control[] { q, a });
                faqBoxes[i] = item;
                pnl.Controls.Add(item);
            }

            relayoutList.Add(() =>
            {
                pnl.Width = _scrollContent.ClientSize.Width;
                int totalW = pnl.ClientSize.Width;
                int sideMargin = Math.Max(40, (int)(totalW * 0.06));
                int gapX = 24;
                int boxW = (totalW - sideMargin * 2 - gapX) / 2;
                int boxH = 90;

                for (int i = 0; i < 4; i++)
                {
                    int r = i / 2;
                    int c = i % 2;
                    faqBoxes[i].Location = new Point(sideMargin + c * (boxW + gapX), 75 + r * (boxH + 15));
                    faqBoxes[i].Size = new Size(boxW, boxH);
                }
            });

            return pnl;
        }

        // ════════════════════════════════════════════════════════════
        // SECTION 8: CTA CARD (Tăng chiều cao, vẽ chữ trực tiếp không che/đè)
        // ════════════════════════════════════════════════════════════
        private Panel BuildCtaSection(ref int topY, System.Collections.Generic.List<Action> relayoutList)
        {
            int h = 280; // Tăng chiều cao để không gian rộng rãi thoáng mắt
            var pnl = new Panel
            {
                Location = new Point(0, topY),
                Size = new Size(_scrollContent.ClientSize.Width, h),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White
            };
            topY += h;

            var card = new Panel { BackColor = C_Dark };

            var btnRegCta = new Button
            {
                Text = "ĐĂNG KÝ TÀI KHOẢN NGAY  ➔",
                Size = new Size(320, 52),
                BackColor = C_Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRegCta.FlatAppearance.BorderSize = 0;
            btnRegCta.Click += (s, e) => new FormDangKyBenhNhan().ShowDialog();

            // Vẽ tiêu đề và phụ đề trực tiếp vào Canvas card để giãn dòng thoáng, không bị đè nút
            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                using var br = new LinearGradientBrush(card.ClientRectangle,
                    Color.FromArgb(15, 23, 42), Color.FromArgb(30, 41, 59), 45f);
                g.FillRectangle(br, card.ClientRectangle);

                // Title
                using (var fontTitle = new Font("Segoe UI", 24, FontStyle.Bold))
                using (var brushTitle = new SolidBrush(Color.White))
                {
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near };
                    g.DrawString("Sẵn Sàng Chăm Sóc Sức Khỏe Của Bạn?", fontTitle, brushTitle, new RectangleF(0, 32, card.Width, 45), sf);
                }

                // Subtitle
                using (var fontSub = new Font("Segoe UI", 11F))
                using (var brushSub = new SolidBrush(Color.FromArgb(203, 213, 225)))
                {
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near };
                    g.DrawString("Tạo ngay hồ sơ y tế miễn phí và trải nghiệm hệ thống đặt lịch hẹn thông minh, không cần phải chờ đợi.",
                        fontSub, brushSub, new RectangleF(20, 85, card.Width - 40, 35), sf);
                }
            };

            card.Controls.Add(btnRegCta);
            pnl.Controls.Add(card);

            relayoutList.Add(() =>
            {
                pnl.Width = _scrollContent.ClientSize.Width;
                int sideMargin = Math.Max(40, (int)(pnl.ClientSize.Width * 0.06));
                card.Location = new Point(sideMargin, 20);
                card.Size = new Size(pnl.ClientSize.Width - sideMargin * 2, 230);
                btnRegCta.Location = new Point((card.ClientSize.Width - btnRegCta.Width) / 2, 140);
                card.Invalidate();
            });

            return pnl;
        }

        // ════════════════════════════════════════════════════════════
        // SECTION 9: FOOTER
        // ════════════════════════════════════════════════════════════
        private Panel BuildFooterSection(ref int topY, System.Collections.Generic.List<Action> relayoutList)
        {
            int h = 240;
            var pnl = new Panel
            {
                Location = new Point(0, topY),
                Size = new Size(_scrollContent.ClientSize.Width, h),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(15, 23, 42)
            };
            topY += h;

            var lblLogo = new Label
            {
                Text = "❤ MediCore Clinic",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = C_Primary,
                Location = new Point(60, 25),
                AutoSize = true
            };
            var lblAbout = new Label
            {
                Text = "Hệ thống phòng khám đa khoa tiêu chuẩn quốc tế,\nmang đến dịch vụ chăm sóc sức khỏe toàn diện và tận tâm cho mọi gia đình.",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(60, 68),
                Size = new Size(480, 50)
            };

            var lblContactTitle = new Label
            {
                Text = "THÔNG TIN LIÊN HỆ",
                Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
                ForeColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                AutoSize = true
            };
            var lblContact = new Label
            {
                Text = "📍 123 Đường Y Tế, Quận 1, TP. Hồ Chí Minh\n📞 Hotline: 1900 1234 - contact@medicore.vn\n⏰ Giờ làm việc: 7:00 - 20:00 (Thứ 2 - Chủ Nhật)",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(203, 213, 225),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Size = new Size(420, 68)
            };

            var lblCopy = new Label
            {
                Text = "© 2026 MediCore Clinic. All rights reserved. Developed by Dang Quang Doanh",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(0, 190),
                Size = new Size(pnl.Width, 25),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleCenter
            };

            pnl.Controls.AddRange(new Control[] { lblLogo, lblAbout, lblContactTitle, lblContact, lblCopy });

            relayoutList.Add(() =>
            {
                pnl.Width = _scrollContent.ClientSize.Width;
                int rightColX = Math.Max(500, pnl.ClientSize.Width - 460);
                lblContactTitle.Location = new Point(rightColX, 25);
                lblContact.Location = new Point(rightColX, 60);
            });

            return pnl;
        }

        // ════════════════════════════════════════════════════════════
        // HELPER: Bo góc
        // ════════════════════════════════════════════════════════════
        private static GraphicsPath GetRoundedRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
