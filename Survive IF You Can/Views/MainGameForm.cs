using Microsoft.VisualBasic.Devices;
using Survive_IF_You_Can.Properties;
using System.ComponentModel;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using Timer = System.Windows.Forms.Timer;

namespace Survive_IF_You_Can
{
    // ��������� ������������� PlayerDirection
    public enum PlayerDirection
    {
        None,
        Up,
        Down,
        Left,
        Right
    }

    public partial class MainGameForm : Form
    {
        private List<IShooting> bullets = new List<IShooting>();
        private PlayerMovementController movementController;
        private PlayerDirection playerDirection;

        bool goup;
        bool godown;
        bool goleft;
        bool goright;
        string looking = "up";
        double playerHealth = 100;
        int playerSpeed = 7;
        int ammo = 10;
        int zomboSpeed = 1;
        public int kills = 0;
        bool gameOver = false;
        bool isPaused = false;
        Timer pauseTimer;

        Random rand = new Random();
        List<PictureBox> zomboList = new List<PictureBox>();

        GameOverForm gameOverForm = new GameOverForm();

        public MainGameForm()
        {
            InitializeComponent();
            movementController = new PlayerMovementController(this);
            RestartGame();
        }

        private void MainGameForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && ammo > 0)
            {
                ammo--;
                var bullet = ShootingFactory.CreateShooting(looking, Player.Left + (Player.Width / 2), Player.Top + (Player.Height / 2), this);
                bullets.Add(bullet);
                bullet.Shoot();

                if (ammo < 1)
                    DropAmmo();
            }
        }

        private void keyIsDown(object sender, KeyEventArgs e)
        {
            movementController.KeyDown(e);

            if (gameOver) return;

            if (e.KeyCode == Keys.S || e.KeyCode == Keys.Down)
            {
                godown = true;
                looking = "down";
                SetPlayerDirection(PlayerDirection.Down);
                Player.Image = Properties.Resources.BayraktarDOWN;
            }
            else if (e.KeyCode == Keys.W || e.KeyCode == Keys.Up)
            {
                goup = true;
                looking = "up";
                SetPlayerDirection(PlayerDirection.Up);
                Player.Image = Properties.Resources.BayraktarUP;
            }
            else if (e.KeyCode == Keys.A || e.KeyCode == Keys.Left)
            {
                goleft = true;
                looking = "left";
                SetPlayerDirection(PlayerDirection.Left);
                Player.Image = Properties.Resources.BayraktarLEFT;
            }
            else if (e.KeyCode == Keys.D || e.KeyCode == Keys.Right)
            {
                goright = true;
                looking = "right";
                SetPlayerDirection(PlayerDirection.Right);
                Player.Image = Properties.Resources.BayraktarRIGHT;
            }
        }

        private void keyIsUp(object sender, KeyEventArgs e)
        {
            movementController.KeyUp(e);

            if (gameOver == true)
            {
                gameOverForm.Show();
                gameOverForm.ActualScoreLabel.Text = "��� �������: " + this.kills;
            }

            if (e.KeyCode == Keys.S || e.KeyCode == Keys.Down)
            {
                godown = false;
            }
            else if (e.KeyCode == Keys.W || e.KeyCode == Keys.Up)
            {
                goup = false;
            }
            else if (e.KeyCode == Keys.A || e.KeyCode == Keys.Left)
            {
                goleft = false;
            }
            else if (e.KeyCode == Keys.D || e.KeyCode == Keys.Right)
            {
                goright = false;
            }

            if (!goup && !godown && !goleft && !goright)
            {
                ResetPlayerDirection();
            }

            if (e.KeyCode == Keys.Escape)
            {
                if (gameOver)
                {
                    RestartGame();
                }
                else
                {
                    TogglePause();
                }
            }
        }

        private Timer zomboDeadTimer = new Timer();
        private PictureBox deadZomboPictureBox;

        private void GameEngine(object sender, EventArgs e)
        {
            if (playerHealth > 1)
            {
                PBforHealth.Value = Convert.ToInt32(playerHealth);
            }
            else
            {
                Player.Image = Properties.Resources.bayrDead;
                timer.Stop();
                gameOver = true;
            }
            if (playerHealth <= 0)
            {
                // ������� �������
                gameOverForm.Show();
                gameOverForm.ActualScoreLabel.Text = "��� �������: " + this.kills;
                timer.Stop();
                gameOver = true;
                return;
            }
            labelForAmmo.Text = "�������: " + ammo;
            labelForScore.Text = "�������: " + kills;

            UpdatePlayerMovement();

            foreach (Control controlX in this.Controls)
            {
                // ������� ���� �������
                if (controlX is PictureBox && (string)controlX.Tag == "ammo")
                {
                    if (Player.Bounds.IntersectsWith(controlX.Bounds))
                    {
                        this.Controls.Remove(controlX);
                        ((PictureBox)controlX).Dispose();
                        ammo += 5;
                    }
                }

                if (controlX is PictureBox && (string)controlX.Tag == "zombo1")
                {
                    // ���� ��������� �� ����
                    if (Player.Bounds.IntersectsWith(controlX.Bounds))
                    {
                        playerHealth -= 1;
                    }

                    // ���������� (������) ���� �� ���
                    if (controlX.Left > Player.Left)
                    {
                        controlX.Left -= zomboSpeed;
                        ((PictureBox)controlX).Image = Properties.Resources.ZombieLEFT;
                    }

                    if (controlX.Top > Player.Top)
                    {
                        controlX.Top -= zomboSpeed;
                        ((PictureBox)controlX).Image = Properties.Resources.ZombieUP;
                    }

                    if (controlX.Left < Player.Left)
                    {
                        controlX.Left += zomboSpeed;
                        ((PictureBox)controlX).Image = Properties.Resources.ZombieRight;
                    }

                    if (controlX.Top < Player.Top)
                    {
                        controlX.Top += zomboSpeed;
                        ((PictureBox)controlX).Image = Properties.Resources.ZombieDOWN;
                    }
                }

                foreach (Control controlJ in this.Controls)
                {
                    if (controlX is PictureBox && (string)controlX.Tag == "bullet" && controlJ is PictureBox && (string)controlJ.Tag == "zombo1")
                    {
                        // �������� � ����
                        if (controlX.Bounds.IntersectsWith(controlJ.Bounds))
                        {
                            kills++;

                            ((PictureBox)controlJ).Image = null;
                            deadZomboPictureBox = (PictureBox)controlJ;
                            ShowDeadZombo();

                            this.Controls.Remove(controlJ);
                            this.Controls.Remove(controlX);
                            ((PictureBox)controlX).Dispose();

                            zomboList.Remove((PictureBox)controlJ);
                            MakeZombo();
                        }
                    }
                }
            }
        }

        private void ShowDeadZombo()
        {
            deadZomboPictureBox.Image = Properties.Resources.ZomboDead;
            zomboDeadTimer.Start();
        }

        private void zomboDeadTimer_Tick(object sender, EventArgs e)
        {
            zomboDeadTimer.Stop();
            this.Controls.Remove(deadZomboPictureBox);
        }

        private void DropAmmo()
        {
            PictureBox ammo = new PictureBox();
            ammo.Tag = "ammo";
            ammo.Image = Properties.Resources.ammoIMG;
            ammo.SizeMode = PictureBoxSizeMode.AutoSize;
            ammo.Left = rand.Next(10, this.ClientSize.Width - ammo.Width);
            ammo.Top = rand.Next(50, this.ClientSize.Height - ammo.Height);
            this.Controls.Add(ammo);

            ammo.BringToFront();
        }

        private void MakeZombo()
        {
            PictureBox zombo = new PictureBox();
            zombo.Tag = "zombo1";
            zombo.Image = Properties.Resources.ZombieLEFT;
            zombo.Left = rand.Next(0, 1200);
            zombo.Top = rand.Next(0, 780);
            zombo.SizeMode = PictureBoxSizeMode.AutoSize;
            zomboList.Add(zombo);
            this.Controls.Add(zombo);
            Player.BringToFront();
        }

        private void BringTopPanelToFront()
        {
            labelForAmmo.BringToFront();
            labelForScore.BringToFront();
            labelForHealth.BringToFront();
            PBforHealth.BringToFront();
        }

        private void RestartGame()
        {
            timer.Stop();

            zomboDeadTimer.Interval = 1000;
            zomboDeadTimer.Tick += zomboDeadTimer_Tick;

            Player.Image = Properties.Resources.BayraktarUP;

            foreach (PictureBox zomb in zomboList)
            {
                this.Controls.Remove(zomb);
            }

            zomboList.Clear();

            for (int zomb = 0; zomb < 3; zomb++)
            {
                MakeZombo();
            }

            goup = false;
            godown = false;
            goleft = false;
            goright = false;

            playerHealth = 100;
            kills = 0;
            ammo = 10;

            timer.Start();
        }

        private void MainGameForm_Load(object sender, EventArgs e)
        {
            movementController = new PlayerMovementController(this);
        }

        private void MainGameForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.Exit();
        }

        // ��������� ����� � ��
        private void TogglePause()
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }

        PauseForm pause = new PauseForm();

        private void PauseGame()
        {
            isPaused = true;
            timer.Stop();

            using (var overlayForm = new TransparentOverlayForm())
            {
                overlayForm.Bounds = Bounds;
                overlayForm.TopMost = true;
                overlayForm.ShowInTaskbar = false;
                overlayForm.Show();

                pause.Show();
                pause.scoreLabel.Text = "��� �������: " + this.kills;

                foreach (Control control in Controls)
                {
                    control.Enabled = false;
                }

                pauseTimer = new Timer();
                pauseTimer.Interval = 100;
                pauseTimer.Tick += PauseTimer_Tick;
                pauseTimer.Start();
            }
        }

        private void ResumeGame()
        {
            timer.Stop();

            isPaused = false;
            pauseTimer.Stop();
            pauseTimer.Dispose();
            PauseForm pause = new PauseForm();
            pause.Hide();

            foreach (Control control in Controls)
            {
                control.Enabled = true;
            }

            timer.Start();
        }

        public class TransparentOverlayForm : Form
        {
            public TransparentOverlayForm()
            {
                SetStyle(ControlStyles.SupportsTransparentBackColor, true);
                BackColor = Color.Transparent;
            }
        }

        private bool IsEscapeKeyPressed()
        {
            PauseForm pause = new PauseForm();
            return (Control.ModifierKeys & Keys.Escape) == Keys.Escape;
        }

        private void PauseTimer_Tick(object sender, EventArgs e)
        {
            if (IsEscapeKeyPressed())
            {
                ResumeGame();
            }
        }

        // ��������� ���� ������
        private void UpdatePlayerMovement()
        {
            switch (playerDirection)
            {
                case PlayerDirection.Up:
                    if (Player.Top > 0)
                        Player.Top -= playerSpeed;
                    break;
                case PlayerDirection.Down:
                    if (Player.Top + Player.Height < ClientSize.Height)
                        Player.Top += playerSpeed;
                    break;
                case PlayerDirection.Left:
                    if (Player.Left > 0)
                        Player.Left -= playerSpeed;
                    break;
                case PlayerDirection.Right:
                    if (Player.Left + Player.Width < ClientSize.Width)
                        Player.Left += playerSpeed;
                    break;
                default:
                    break;
            }
        }

        // ������������ �������� ���� ������
        private void SetPlayerDirection(PlayerDirection direction)
        {
            playerDirection = direction;
        }

        // �������� �������� ���� ������
        private void ResetPlayerDirection()
        {
            playerDirection = PlayerDirection.None;
        }
    }
}