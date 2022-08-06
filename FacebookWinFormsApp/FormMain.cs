using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using FacebookWrapper.ObjectModel;
using FacebookWrapper;
using FacebookLogic;

namespace BasicFacebookFeatures
{
    public sealed partial class FormMain : Form
    {
        private const string k_AppID = "204543351599299";
        private static readonly object sr_Lock = new object();
        private static FormMain s_FormMainInstance = null;
        private User m_LoggedInUser;
        private LoginResult m_LoginResult;
        private AppSettings m_AppSettings;
        private bool m_Visibility = true;
        private FormExit m_FormExit = new FormExit();

        private FormMain()
        {
            this.InitializeComponent();
            m_AppSettings = AppSettings.LoadFromFile();
            this.Size = m_AppSettings.LastWindowSize;
            this.Location = m_AppSettings.LastWindowLocation;
            m_FormExit.m_ExitFormNotifier += exitApp;
            m_FormExit.m_RememberChangedNotifier += () => m_AppSettings.RememberUser = m_FormExit.RememberMe;
            m_FormExit.RememberMe = m_AppSettings.RememberUser;
            FacebookWrapper.FacebookService.s_CollectionLimit = 100;
        }

        private void exitApp()
        {
            m_AppSettings.LastWindowLocation = this.Location;
            m_AppSettings.LastWindowSize = this.Size;

            if (m_AppSettings.RememberUser)
            {
                m_AppSettings.LastAccessToken = m_LoginResult.AccessToken;
            }
            else
            {
                m_AppSettings.LastAccessToken = null;
            }

            m_AppSettings.SaveToFile();
            this.Close();
        }

        public static FormMain FormMainInstance
        {
            get
            {
                if (s_FormMainInstance == null)
                {
                    lock (sr_Lock)
                    {
                        if (s_FormMainInstance == null)
                        {
                            s_FormMainInstance = new FormMain();
                        }
                    }
                }

                return s_FormMainInstance;
            }
        }

        private void loginUser()
        {
            if (buttonLogInAndOut.Text.Equals("Login"))
            {
                m_LoginResult = FacebookService.Login(
                    k_AppID,
                    "email",
                    "public_profile",
                    "user_age_range",
                    "user_birthday",
                    "user_events",
                    "user_friends",
                    "user_gender",
                    "user_hometown",
                    "user_likes",
                    "user_link",
                    "user_location",
                    "user_photos",
                    "user_posts",
                    "user_videos");

                if (!string.IsNullOrEmpty(m_LoginResult.AccessToken))
                {
                    m_LoggedInUser = m_LoginResult.LoggedInUser;
                    FacebookLogicManager.LoggedInUser = m_LoggedInUser;
                    changeVisibilityOfAllControls(true);
                    textBoxStatus.Text += $", {m_LoginResult.LoggedInUser.Name}?";
                    new Thread(fetchUserInfo).Start();
                    buttonLogInAndOut.Text = "Logout";
                }
                else
                {
                    MessageBox.Show("Login Failed", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else 
            {
                FacebookService.Logout(disconnectUser);
                buttonLogInAndOut.Text = "Login";
            }
        }

        private void disconnectUser()
        {
            this.Text = "Facebook App";
            checkBoxRememberMe.Checked = false;
            pictureBoxProfile.ImageLocation = null;
            pictureBoxSelectedPicture.ImageLocation = null;
            textBoxStatus.Text = string.Empty;
            clearDataSource(friendBindingSource);
            clearDataSource(albumsBindingSource);
            clearDataSource(photosBindingSource);
            clearDataSource(userBindingSource);
            clearDataSource(postProxyBindingSource);
            listBoxPostsFromThePast.Items.Clear();
            checkedListBoxFriendsWhoHasBirthdayToday.Items.Clear();
            textBoxPostFromThePast.Text = string.Empty;
            textBoxWriteYourWish.Text = string.Empty;
            checkedListBoxOpptionalWishes.Items.Clear();
            changeVisibilityOfAllControls(!m_Visibility);
            FacebookLogicManager.LoggedInUser = null;
        }

        private void clearDataSource(BindingSource i_DataSource)
        {
            i_DataSource.DataSource = null;
        }

        private bool isBindingSourceBind(BindingSource i_DataSource)
        {
            return i_DataSource.Count > 0;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            if (m_AppSettings.RememberUser
                && !string.IsNullOrEmpty(m_AppSettings.LastAccessToken))
            {
                new Thread(autoLogin).Start();
                buttonLogInAndOut.Text = "Logout";
            }
        }

        private void autoLogin()
        {
            try
            {
                m_LoginResult = FacebookService.Connect(m_AppSettings.LastAccessToken);
                m_LoggedInUser = m_LoginResult.LoggedInUser;
                FacebookLogicManager.LoggedInUser = m_LoggedInUser;
                this.Invoke(new Action(() => this.Text = string.Format("Logged In As: {0}", m_LoggedInUser.Name)));
                this.Invoke(new Action(() => changeVisibilityOfAllControls(m_Visibility)));
                new Thread(fetchUserInfo).Start();
            }
            catch (Exception)
            {
                this.Invoke((Action)loginUser);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!m_FormExit.ToExit)
            {
                m_FormExit.ShowDialog();
                e.Cancel = true;
            }
        }

        private void changeVisibilityOfAllControls(bool i_Visibility)
        {
            foreach (Control control in this.Controls)
            {
                changeVisibility(control, i_Visibility);
            }
        }

        private void changeVisibility(Control control, bool i_Visibility)
        {
            if (control.HasChildren)
            {
                foreach (Control child in control.Controls)
                {
                    changeVisibility(child, i_Visibility);
                }
            }

            control.Visible = i_Visibility;
        }

        private void fetchUserInfo()
        {   
            this.Invoke(new Action(() => userBindingSource.DataSource = m_LoggedInUser));
            this.Invoke(new Action(() => pictureBoxProfile.LoadAsync(m_LoggedInUser.PictureNormalURL)));
        }

        private void buttonLogInAndOut_Click(object sender, EventArgs e)
        {
            Clipboard.SetText("design.patterns.c21ב");
            loginUser();
        }

        private void buttonFetchFriends_Click(object sender, EventArgs e)
        {
            new Thread(fetchFriends).Start();
        }

        private void fetchFriends()
        {
            if (!isBindingSourceBind(friendBindingSource))
            {
                listBoxFriends.Invoke(new Action(() => friendBindingSource.DataSource = m_LoggedInUser.Friends));
            }

            if (m_LoggedInUser.Friends.Count == 0)
            {
                MessageBox.Show("There Are No Friends", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void buttonFetchPosts_Click(object sender, EventArgs e)
        {
            new Thread(fetchPosts).Start();
        }

        private void fetchPosts()
        {
            if (!isBindingSourceBind(postProxyBindingSource))
            {
                listBoxPosts.Invoke(new Action(() => postProxyBindingSource.DataSource = FacebookLogicManager.GetProxyPosts()));
            }

            if (listBoxPosts.Items.Count == 0)
            {
                MessageBox.Show("There Is No Posts In Feed", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void buttonFetchAlbums_Click(object sender, EventArgs e)
        {
            new Thread(fetchAlbums).Start();
        }

        private void fetchAlbums()
        {
            if (!isBindingSourceBind(albumsBindingSource))
            {
                listBoxAlbums.Invoke(new Action(() => albumsBindingSource.DataSource = m_LoggedInUser.Albums));
            }

            if (listBoxAlbums.Items.Count == 0)
            {
                MessageBox.Show("There Is No Albums", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void listBoxAlbums_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.listBoxAlbums.SelectedItem is Album selectedAlbum)
            {
                new Thread(displaySelectedAlbum).Start();
            }
        }

        private void displaySelectedAlbum()
        {
            listBoxAlbums.Invoke(new Action(() =>
            {
                Album selectedAlbum = listBoxAlbums.SelectedItem as Album;
                listBoxPhotosInSelectedAlbum.Invoke(new Action(() => photosBindingSource.DataSource = selectedAlbum.Photos));
                if (selectedAlbum.Photos.Count == 0)
                {
                    MessageBox.Show("No Picture Albums To Retrieve...", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }));
        }

        private void listBoxPhotosInSelectedAlbum_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.listBoxPhotosInSelectedAlbum.SelectedItem is Photo selectedPhoto)
            {
                new Thread(displaySelectedPictureInAlbum).Start();
            }
        }

        private void displaySelectedPictureInAlbum()
        {
            listBoxPhotosInSelectedAlbum.Invoke(new Action(() =>
            {
                Photo selectedPicture = listBoxPhotosInSelectedAlbum.SelectedItem as Photo;
                if (selectedPicture.PictureNormalURL != null)
                {
                    pictureBoxSelectedPicture.LoadAsync(selectedPicture.PictureNormalURL);
                }
                else
                {
                    pictureBoxSelectedPicture.Image = pictureBoxSelectedPicture.ErrorImage;
                }
            }));
        }

        private void buttonFetchEvents_Click(object sender, EventArgs e)
        {
            new Thread(fetchEvents).Start();
        }

        private void fetchEvents()
        {
            if (!isBindingSourceBind(eventsBindingSource))
            {
                listBoxEvents.Invoke(new Action(() => eventsBindingSource.DataSource = m_LoggedInUser.Events));
            }

            if (listBoxEvents.Items.Count == 0)
            {
                MessageBox.Show("There Are No Events", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void buttonFetchGroups_Click(object sender, EventArgs e)
        {
            new Thread(fetchGroups).Start();
        }

        private void fetchGroups()
        {
            if (!isBindingSourceBind(groupsBindingSource))
            {
                listBoxGroups.Invoke(new Action(() => groupsBindingSource.DataSource = m_LoggedInUser.Groups));
            }

            if (listBoxGroups.Items.Count == 0)
            {
                MessageBox.Show("There Are No Groups", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void textBoxStatus_MouseClick(object sender, MouseEventArgs e)
        {
            this.textBoxStatus.Text = string.Empty;
        }

        private void buttonPost_Click(object sender, EventArgs e)
        {
            new Thread(postStatus).Start();
        }

        private void postStatus()
        {
            try
            {
                Status postedStatus = m_LoggedInUser.PostStatus(textBoxStatus.Text);
            }
            catch(Exception)
            {
                MessageBox.Show("Status Post Has Failed", "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                this.Invoke(
                    new Action(
                        () =>
                            {
                                textBoxStatus.Text = "What's on your mind";
                                this.buttonPost.Enabled = false;
                                this.textBoxStatus.Enabled = false;
                                textBoxStatus.ForeColor = SystemColors.ControlDark;
                            }));
            }
        }

        private void textBoxWriteYourWish_MouseClick(object sender, MouseEventArgs e)
        {
            if (this.textBoxWriteYourWish.Text == "write your own wish..")
            {
                this.textBoxWriteYourWish.Text = string.Empty;
            }
        }

        private void buttonFetchFriendsWhoHasBirthdayToday_Click(object sender, EventArgs e)
        {
            new Thread(fetchFriendsWhoHasBirthday).Start();
        }

        private void fetchFriendsWhoHasBirthday()
        {
            List<User> friendsWhoHasBirthdayTodayList = FacebookLogicManager.FetchFriendsWhoHaveBirthdayToday();

            this.checkedListBoxFriendsWhoHasBirthdayToday.Invoke(new Action(() =>
            {
                this.checkedListBoxFriendsWhoHasBirthdayToday.Items.Clear();
                this.checkedListBoxFriendsWhoHasBirthdayToday.DisplayMember = "Name";

                foreach (User friend in friendsWhoHasBirthdayTodayList)
                {
                    this.checkedListBoxFriendsWhoHasBirthdayToday.Items.Add(friend);
                }
            }));

            if (friendsWhoHasBirthdayTodayList.Count == 0)
            {
                MessageBox.Show("You Don't Have Any Friends Who Has Birthday Today", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void buttonCreateBirthdayWish_Click(object sender, EventArgs e)
        {
            if (this.checkedListBoxOpptionalWishes.SelectedItems.Count != 0)
            {
                createBirthdayWish();
            }
            else
            {
                MessageBox.Show(
                    "You Have Not Checked Any Wishes",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private void createBirthdayWish()
        {
            StringBuilder wishes = new StringBuilder();
            foreach (string wish in this.checkedListBoxOpptionalWishes.CheckedItems)
            {
                wishes.AppendFormat(" " + wish);
            }

            this.textBoxWriteYourWish.Text = wishes.ToString();
        }

        private void buttonFetchPhotosFromThePast_Click(object sender, EventArgs e)
        {
            listBoxPhotosFromThePast.Items.Clear();
            listBoxPhotosFromThePast.DisplayMember = "CreatedTime.Value.ToString()";

            List<Photo> photosFromThePastList = FacebookLogicManager.GetMemoryPhotosFromThePast();
            foreach (Photo photo in photosFromThePastList)
            {
                listBoxPhotosFromThePast.Items.Add(photo);
            }

            if (photosFromThePastList.Count == 0)
            {
                MessageBox.Show("There Are No Photos From The Past", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void buttonFetchPostsFromThePast_Click(object sender, EventArgs e)
        {
            listBoxPostsFromThePast.Items.Clear();
            listBoxPostsFromThePast.DisplayMember = "From.Name";

            PostProxy.PostProxyCollection postsFromThePastList = FacebookLogicManager.GetMemoryPostsFromThePast();
            listBoxPostsFromThePast.Invoke(new Action(() =>
                {
                    listBoxPostsFromThePast.Items.Clear();
                    foreach (PostProxy post in postsFromThePastList)
                    {
                        listBoxPostsFromThePast.Items.Add(post);
                    }
                }));

            if (postsFromThePastList.IsNullOrEmpty())
            {
                MessageBox.Show("There Are No Posts From The Past", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void listBoxPhotosFromThePast_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.displaySelectedPicture();
        }

        private void displaySelectedPicture()
        {
            if (listBoxPhotosFromThePast.SelectedItems.Count == 1)
            {
                Photo selectedPicture = listBoxPhotosFromThePast.SelectedItem as Photo;
                if (selectedPicture != null && selectedPicture.PictureNormalURL != null)
                {
                    pictureBoxPhotoFromThePast.LoadAsync(selectedPicture.PictureNormalURL);
                }
                else
                {
                    pictureBoxPhotoFromThePast.Image = pictureBoxPhotoFromThePast.ErrorImage;
                }
            }
        }

        private void listBoxPostFromThePast_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.displaySelectedPost();
        }

        private void displaySelectedPost()
        {
            if (listBoxPostsFromThePast.SelectedItems.Count == 1)
            {
                Post selectedPost = listBoxPostsFromThePast.SelectedItem as Post;
                if (selectedPost != null)
                {
                    textBoxPostFromThePast.Text = selectedPost.Caption;
                }
                else
                {
                    textBoxPostFromThePast.Text = "An Error Occurred.";
                }
            }
        }

        private void buttonPostWish_Click(object sender, EventArgs e)
        {
            string wishes = textBoxWriteYourWish.Text;
            if (this.checkedListBoxFriendsWhoHasBirthdayToday.Items.Count == 0)
            {
                MessageBox.Show(
                    "You Don't Have Any Friends Who Has Birthday Today",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else if (this.checkedListBoxFriendsWhoHasBirthdayToday.SelectedItems.Count == 0)
            {
                MessageBox.Show(
                    "You Have Not Checked Any Friend",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else if (string.IsNullOrEmpty(wishes))
            {
                MessageBox.Show(
                    "There Is No Blessing To Send",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                foreach (User friendWhoHasBirthdayToday in this.checkedListBoxFriendsWhoHasBirthdayToday.SelectedItems)
                {
                    try
                    {
                        friendWhoHasBirthdayToday.PostStatus(wishes);
                    }
                    catch (Exception)
                    {
                        MessageBox.Show(
                            "The Birthday Wish Has Failed To Send",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                }
            }
        }
    }
}