﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using UniversalSoundBoard.Models;
using UniversalSoundBoard.Pages;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace UniversalSoundBoard.DataAccess
{
    public class FileManager
    {
        #region Variables
        public const double volume = 1.0;
        public const bool liveTile = true;
        public const bool playingSoundsListVisible = true;
        public const bool playOneSoundAtOnce = false;
        public const string theme = "system";
        public const bool showCategoryIcon = true;
        public const bool showSoundsPivot = true;
        public const int mobileMaxWidth = 550;
        public const int tabletMaxWidth = 650;
        public const int topButtonsCollapsedMaxWidth = 1400;
        public const int sideBarCollapsedMaxWidth = 1100;
        public const int moveSelectButtonMaxWidth = 850;
        public const int moveAddButtonMaxWidth = 800;
        public const int moveVolumeButtonMaxWidth = 750;
        public const int hideSearchBoxMaxWidth = 700;

        public static bool skipAutoSuggestBoxTextChanged = false;
        #endregion

        #region Filesystem Methods
        public static async Task<StorageFolder> GetSoundsFolderAsync()
        {
            StorageFolder root = ApplicationData.Current.LocalFolder;
            StorageFolder detailsFolder;
            string soundsFolderName = "sounds";
            if (await root.TryGetItemAsync(soundsFolderName) == null)
            {
                return detailsFolder = await root.CreateFolderAsync(soundsFolderName);
            }
            else
            {
                return detailsFolder = await root.GetFolderAsync(soundsFolderName);
            }
        }

        public static async Task<StorageFolder> GetImagesFolderAsync()
        {
            // Create images folder if not exists
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFolder imagesFolder;
            string imagesFolderName = "images";
            if (await folder.TryGetItemAsync(imagesFolderName) == null)
            {
                return imagesFolder = await folder.CreateFolderAsync(imagesFolderName);
            }
            else
            {
                return imagesFolder = await folder.GetFolderAsync(imagesFolderName);
            }
        }
        #endregion

        #region Database Methods
        private static async Task<List<Sound>> GetSavedSounds()
        {
            List<object> soundObjects = DatabaseOperations.GetAllSounds();
            List<Sound> sounds = new List<Sound>();
            StorageFolder soundsFolder = await GetSoundsFolderAsync();
            StorageFolder imagesFolder = await GetImagesFolderAsync();

            foreach (object obj in soundObjects)
            {
                string uuid = obj.GetType().GetProperty("uuid").GetValue(obj).ToString();
                string name = obj.GetType().GetProperty("name").GetValue(obj).ToString();
                bool favourite = obj.GetType().GetProperty("favourite").GetValue(obj).ToString().ToLower() == "true";
                string sound_ext = obj.GetType().GetProperty("sound_ext").GetValue(obj).ToString();
                string image_ext = obj.GetType().GetProperty("image_ext").GetValue(obj).ToString();
                string category_id = obj.GetType().GetProperty("category_id").GetValue(obj).ToString();

                Sound sound = new Sound(uuid, name, favourite);

                // Get the category of the sound
                if (!String.IsNullOrEmpty(category_id))
                {
                    var foundCategories = (App.Current as App)._itemViewHolder.categories.Where(cat => cat.Uuid == category_id);
                    if (foundCategories.Count() > 0)
                    {
                        sound.Category = foundCategories.First();
                    }
                }


                // Get Image for Sound
                BitmapImage image = new BitmapImage();

                Uri defaultImageUri;
                if ((App.Current as App).RequestedTheme == ApplicationTheme.Dark)
                {
                    defaultImageUri = new Uri("ms-appx:///Assets/Images/default-dark.png", UriKind.Absolute);
                }
                else
                {
                    defaultImageUri = new Uri("ms-appx:///Assets/Images/default.png", UriKind.Absolute);
                }
                image.UriSource = defaultImageUri;

                if (!String.IsNullOrEmpty(image_ext))
                {
                    string imageFileName = uuid + "." + image_ext;

                    try
                    {
                        StorageFile imageFile = await imagesFolder.GetFileAsync(imageFileName);

                        Uri uri = new Uri(imageFile.Path, UriKind.Absolute);
                        image.UriSource = uri;
                        sound.ImageFile = imageFile;
                    }
                    catch (Exception e) { }
                }
                sound.Image = image;

                // Add the sound file to the sound
                string soundFileName = uuid + "." + sound_ext;
                sound.AudioFile = await soundsFolder.GetFileAsync(soundFileName);

                sounds.Add(sound);
            }
            (App.Current as App)._itemViewHolder.allSoundsChanged = false;
            return sounds;
        }

        public static async Task GetAllSounds()
        {
            (App.Current as App)._itemViewHolder.progressRingIsActive = true;

            (App.Current as App)._itemViewHolder.sounds.Clear();
            (App.Current as App)._itemViewHolder.favouriteSounds.Clear();

            await UpdateAllSoundsList();

            // Get the sounds from itemViewHolder
            foreach (Sound sound in (App.Current as App)._itemViewHolder.allSounds)
            {
                (App.Current as App)._itemViewHolder.sounds.Add(sound);
                if (sound.Favourite)
                {
                    (App.Current as App)._itemViewHolder.favouriteSounds.Add(sound);
                }
            }

            (App.Current as App)._itemViewHolder.progressRingIsActive = false;
        }

        private static async Task UpdateAllSoundsList()
        {
            if ((App.Current as App)._itemViewHolder.allSoundsChanged)
            {
                (App.Current as App)._itemViewHolder.allSounds.Clear();
                foreach (Sound sound in await GetSavedSounds())
                {
                    (App.Current as App)._itemViewHolder.allSounds.Add(sound);
                }
                UpdateLiveTile();
            }
        }

        public static async Task AddSound(Sound sound)
        {
            string uuid = Guid.NewGuid().ToString();
            string ext = sound.AudioFile.FileType.Replace(".", "");

            // Move the file into the sounds folder
            StorageFolder soundsFolder = await GetSoundsFolderAsync();
            StorageFile newFile = await sound.AudioFile.CopyAsync(soundsFolder, uuid + sound.AudioFile.FileType, NameCollisionOption.ReplaceExisting);

            DatabaseOperations.AddSound(uuid, sound.Name, sound.Category.Uuid, ext);
            (App.Current as App)._itemViewHolder.allSoundsChanged = true;
            await GetAllSounds();
        }

        public static async Task GetSoundsByCategory(Category category)
        {
            (App.Current as App)._itemViewHolder.playAllButtonVisibility = Visibility.Collapsed;

            await UpdateAllSoundsList();

            (App.Current as App)._itemViewHolder.sounds.Clear();
            (App.Current as App)._itemViewHolder.favouriteSounds.Clear();
            foreach (var sound in (App.Current as App)._itemViewHolder.allSounds)
            {
                if (sound.Category != null)
                {
                    if (sound.Category.Uuid == category.Uuid)
                    {
                        (App.Current as App)._itemViewHolder.sounds.Add(sound);
                        if (sound.Favourite)
                        {
                            (App.Current as App)._itemViewHolder.favouriteSounds.Add(sound);
                        }
                    }
                }
            }

            ShowPlayAllButton();
        }

        public static async Task GetSoundsByName(string name)
        {
            (App.Current as App)._itemViewHolder.playAllButtonVisibility = Visibility.Collapsed;
            (App.Current as App)._itemViewHolder.sounds.Clear();

            await UpdateAllSoundsList();

            (App.Current as App)._itemViewHolder.sounds.Clear();
            (App.Current as App)._itemViewHolder.favouriteSounds.Clear();
            foreach (var sound in (App.Current as App)._itemViewHolder.allSounds)
            {
                if (sound.Name.ToLower().Contains(name.ToLower()))
                {
                    (App.Current as App)._itemViewHolder.sounds.Add(sound);
                    if (sound.Favourite)
                    {
                        (App.Current as App)._itemViewHolder.favouriteSounds.Add(sound);
                    }
                }
            }

            ShowPlayAllButton();
        }

        public static async Task DeleteSound(string uuid)
        {
            // Find the sound and image file and delete them
            var soundObject = DatabaseOperations.GetSound(uuid);
            if (soundObject != null)
            {
                string image_ext = soundObject.GetType().GetProperty("image_ext").GetValue(soundObject).ToString();
                string sound_ext = soundObject.GetType().GetProperty("sound_ext").GetValue(soundObject).ToString();

                StorageFolder soundsFolder = await GetSoundsFolderAsync();
                StorageFolder imagesFolder = await GetImagesFolderAsync();
                string soundName = uuid + "." + sound_ext;
                string imageName = uuid + "." + image_ext;

                StorageFile soundFile = await soundsFolder.TryGetItemAsync(soundName) as StorageFile;
                if (soundFile != null)
                    await soundFile.DeleteAsync();

                StorageFile imageFile = await imagesFolder.TryGetItemAsync(imageName) as StorageFile;
                if (imageFile != null)
                    await imageFile.DeleteAsync();

                // Delete Sound from database
                DatabaseOperations.DeleteSound(uuid);
            }
            (App.Current as App)._itemViewHolder.allSoundsChanged = true;
            await GetAllSounds();
        }

        public static void RenameSound(string uuid, string newName)
        {
            DatabaseOperations.UpdateSound(uuid, newName, null, null, null, null);
            (App.Current as App)._itemViewHolder.allSoundsChanged = true;
        }

        public static void SetCategoryOfSound(string soundUuid, string categoryUuid)
        {
            DatabaseOperations.UpdateSound(soundUuid, null, categoryUuid, null, null, null);
            (App.Current as App)._itemViewHolder.allSoundsChanged = true;
        }

        public static void SetSoundAsFavourite(string uuid, bool favourite)
        {
            DatabaseOperations.UpdateSound(uuid, null, null, null, null, favourite.ToString());
        }

        public static async void AddImage(string uuid, StorageFile file)
        {
            StorageFolder imagesFolder = await GetImagesFolderAsync();
            StorageFile newFile = await file.CopyAsync(imagesFolder, uuid + file.FileType, NameCollisionOption.ReplaceExisting);
            string imageExt = file.FileType.Replace(".", "");

            DatabaseOperations.UpdateSound(uuid, null, null, null, imageExt, null);

            (App.Current as App)._itemViewHolder.allSoundsChanged = true;
            await GetAllSounds();
        }

        public static void AddCategory(string name, string icon)
        {
            DatabaseOperations.AddCategory(name, icon);
            CreateCategoriesObservableCollection();
        }

        public static List<Category> GetAllCategories()
        {
            return DatabaseOperations.GetCategories();
        }

        public static void UpdateCategory(string uuid, string name, string icon)
        {
            DatabaseOperations.UpdateCategory(uuid, name, icon);
            CreateCategoriesObservableCollection();
        }

        public static void DeleteCategory(string uuid)
        {
            DatabaseOperations.DeleteCategory(uuid);
            CreateCategoriesObservableCollection();
        }
        #endregion

        #region UI Methods
        public static bool AreTopButtonsNormal()
        {
            if ((App.Current as App)._itemViewHolder.normalOptionsVisibility)
            {
                if ((App.Current as App)._itemViewHolder.searchAutoSuggestBoxVisibility && Window.Current.Bounds.Width >= hideSearchBoxMaxWidth)
                {
                    return true;
                }

                if ((App.Current as App)._itemViewHolder.searchButtonVisibility && Window.Current.Bounds.Width < hideSearchBoxMaxWidth)
                {
                    return true;
                }
            }

            return false;
        }

        public static void SetBackButtonVisibility(bool visible)
        {
            if (visible)
            {
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
                (App.Current as App)._itemViewHolder.windowTitleMargin = new Thickness(60, 7, 0, 0);
            }
            else
            {
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
                (App.Current as App)._itemViewHolder.windowTitleMargin = new Thickness(12, 7, 0, 0);
            }
        }

        public static void CheckBackButtonVisibility()
        {
            if (FileManager.AreTopButtonsNormal() && (App.Current as App)._itemViewHolder.selectedCategory == 0)
            {       // Anything is normal, SoundPage shows All Sounds
                FileManager.SetBackButtonVisibility(false);
            }
            else
            {
                FileManager.SetBackButtonVisibility(true);
            }
        }

        public static async Task ShowAllSounds()
        {
            if (AreTopButtonsNormal())
            {
                SetBackButtonVisibility(false);
            }
            skipAutoSuggestBoxTextChanged = true;
            (App.Current as App)._itemViewHolder.searchQuery = "";
            (App.Current as App)._itemViewHolder.selectedCategory = 0;
            (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
            (App.Current as App)._itemViewHolder.title = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds");
            (App.Current as App)._itemViewHolder.page = typeof(SoundPage);
            await GetAllSounds();
            skipAutoSuggestBoxTextChanged = false;
        }

        public static void AdjustLayout()
        {
            double width = Window.Current.Bounds.Width;

        (App.Current as App)._itemViewHolder.topButtonsCollapsed = (width<topButtonsCollapsedMaxWidth);
            (App.Current as App)._itemViewHolder.selectButtonVisibility = !(width<moveSelectButtonMaxWidth);
            (App.Current as App)._itemViewHolder.addButtonVisibility = !(width<moveAddButtonMaxWidth);
            (App.Current as App)._itemViewHolder.volumeButtonVisibility = !(width<moveVolumeButtonMaxWidth);
            (App.Current as App)._itemViewHolder.shareButtonVisibility = !(width<moveAddButtonMaxWidth);
            (App.Current as App)._itemViewHolder.cancelButtonVisibility = !(width<hideSearchBoxMaxWidth);
            (App.Current as App)._itemViewHolder.moreButtonVisibility = (width<moveSelectButtonMaxWidth
                                                                        || !(App.Current as App)._itemViewHolder.normalOptionsVisibility);

            if (String.IsNullOrEmpty((App.Current as App)._itemViewHolder.searchQuery))
            {
                (App.Current as App)._itemViewHolder.searchAutoSuggestBoxVisibility = !(width<hideSearchBoxMaxWidth);
                (App.Current as App)._itemViewHolder.searchButtonVisibility = (width<hideSearchBoxMaxWidth);
            }

    CheckBackButtonVisibility();
        }

        public static void ShowPlayAllButton()
        {
            if ((App.Current as App)._itemViewHolder.page != typeof(SoundPage)
                || (App.Current as App)._itemViewHolder.progressRingIsActive
                || (App.Current as App)._itemViewHolder.page != typeof(SoundPage))
            {
                (App.Current as App)._itemViewHolder.playAllButtonVisibility = Visibility.Collapsed;
            }
            else
            {
                (App.Current as App)._itemViewHolder.playAllButtonVisibility = Visibility.Visible;
            }
        }

        public static async Task UpdateGridView()
        {
            int selectedCategoryIndex = (App.Current as App)._itemViewHolder.selectedCategory;
            Category selectedCategory = (App.Current as App)._itemViewHolder.categories[selectedCategoryIndex];

            if (selectedCategory != null)
            {
                if ((App.Current as App)._itemViewHolder.searchQuery == "")
                {
                    if (selectedCategoryIndex == 0)
                    {
                        await GetAllSounds();
                        (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
                    }
                    else if ((App.Current as App)._itemViewHolder.page != typeof(SoundPage))
                    {
                        (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
                    }
                    else
                    {
                        await GetSoundsByCategory(selectedCategory);
                        (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Visible;
                    }
                }
                else
                {
                    GetSoundsByName((App.Current as App)._itemViewHolder.searchQuery);
                    (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
                }
            }
            else
            {
                await GetAllSounds();
                (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
            }

            // Check if another category was selected
            if (selectedCategoryIndex != (App.Current as App)._itemViewHolder.selectedCategory)
            {
                // Update UI
                await UpdateGridView();
            }
            ShowPlayAllButton();
        }

        public static async Task ShowCategory(string uuid)
        {
            Category category = DatabaseOperations.GetCategory(uuid);
            if(category != null)
            {
                skipAutoSuggestBoxTextChanged = true;
                (App.Current as App)._itemViewHolder.searchQuery = "";
                (App.Current as App)._itemViewHolder.page = typeof(SoundPage);
                (App.Current as App)._itemViewHolder.title = WebUtility.HtmlDecode(category.Name);
                SetBackButtonVisibility(true);
                (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Visible;
                await GetSoundsByCategory(category);
                SelectCategory(category.Uuid);
            }
            else
            {
                await ShowAllSounds();
            }
        }

        public static void ResetSearchArea()
        {
            skipAutoSuggestBoxTextChanged = true;
            (App.Current as App)._itemViewHolder.searchQuery = "";

            if (Window.Current.Bounds.Width < hideSearchBoxMaxWidth)
            {
                // Clear text and show buttons
                (App.Current as App)._itemViewHolder.searchAutoSuggestBoxVisibility = false;
                (App.Current as App)._itemViewHolder.searchButtonVisibility = true;
            }
            AdjustLayout();
        }

        public static void SwitchSelectionMode()
        {
            if ((App.Current as App)._itemViewHolder.selectionMode == ListViewSelectionMode.None)
            {   // If Normal view
                (App.Current as App)._itemViewHolder.selectionMode = ListViewSelectionMode.Multiple;
                (App.Current as App)._itemViewHolder.normalOptionsVisibility = false;
                (App.Current as App)._itemViewHolder.areSelectButtonsEnabled = false;
            }
            else
            {   // If selection view
                (App.Current as App)._itemViewHolder.selectionMode = ListViewSelectionMode.None;
                (App.Current as App)._itemViewHolder.selectedSounds.Clear();
                (App.Current as App)._itemViewHolder.normalOptionsVisibility = true;
                (App.Current as App)._itemViewHolder.areSelectButtonsEnabled = true;

                if (!String.IsNullOrEmpty((App.Current as App)._itemViewHolder.searchQuery))
                {
                    (App.Current as App)._itemViewHolder.searchAutoSuggestBoxVisibility = true;
                    (App.Current as App)._itemViewHolder.searchButtonVisibility = false;
                }
            }
            AdjustLayout();
        }

        public static void ResetTopButtons()
        {
            if ((App.Current as App)._itemViewHolder.selectionMode != ListViewSelectionMode.None)
            {
                SwitchSelectionMode();
            }
            else
            {
                ResetSearchArea();
            }
        }

        public static void GoBack()
        {
            if (!AreTopButtonsNormal())
            {
                ResetTopButtons();
            }
            else
            {
                if ((App.Current as App)._itemViewHolder.page != typeof(SoundPage))
                {   // If Settings Page is visible
                    // Go to All sounds page
                    (App.Current as App)._itemViewHolder.page = typeof(SoundPage);
                    (App.Current as App)._itemViewHolder.selectedCategory = 0;
                    (App.Current as App)._itemViewHolder.title = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds");
                    ShowAllSounds();
                    (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
                }
                else if ((App.Current as App)._itemViewHolder.selectedCategory == 0)
                {   // If SoundPage shows AllSounds

                }
                else
                {   // If SoundPage shows Category or search results
                    // Top Buttons are normal, but page shows Category or search results
                    ShowAllSounds();
                }
            }

            CheckBackButtonVisibility();
        }
        #endregion

        #region General Methods
        public static void CreateCategoriesObservableCollection()
        {
            (App.Current as App)._itemViewHolder.categories.Clear();
            (App.Current as App)._itemViewHolder.categories.Add(new Category { Name = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds"), Icon = "\uE10F" });

            foreach (Category cat in DatabaseOperations.GetCategories())
            {
                (App.Current as App)._itemViewHolder.categories.Add(cat);
            }
            (App.Current as App)._itemViewHolder.selectedCategory = 0;
        }

        public static void SelectCategory(string uuid)
        {
            for (int i = 0; i < (App.Current as App)._itemViewHolder.categories.Count(); i++)
            {
                if ((App.Current as App)._itemViewHolder.categories[i].Uuid == uuid)
                {
                    (App.Current as App)._itemViewHolder.selectedCategory = i;
                }
            }
        }

        public static void UpdateLiveTile()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            bool isLiveTileOn = false;

            if (localSettings.Values["liveTile"] == null)
            {
                localSettings.Values["liveTile"] = liveTile;
                isLiveTileOn = liveTile;
            }
            else
            {
                isLiveTileOn = (bool)localSettings.Values["liveTile"];
            }

            if ((App.Current as App)._itemViewHolder.allSounds.Count == 0 || !isLiveTileOn)
            {
                TileUpdateManager.CreateTileUpdaterForApplication().Clear();
                return;
            }
            
            List<Sound> sounds = new List<Sound>();
            // Get sound with image
            foreach(Sound s in (App.Current as App)._itemViewHolder.allSounds.Where(s => s.ImageFile != null))
            {
                sounds.Add(s);
            }

            Sound sound;
            if (sounds.Count == 0)
            {
                return;
            }
            else
            {
                Random random = new Random();
                sound = sounds.ElementAt(random.Next(sounds.Count));
            }

            NotificationsExtensions.Tiles.TileBinding binding = new NotificationsExtensions.Tiles.TileBinding()
            {
                Branding = NotificationsExtensions.Tiles.TileBranding.NameAndLogo,

                Content = new NotificationsExtensions.Tiles.TileBindingContentAdaptive()
                {
                    PeekImage = new NotificationsExtensions.Tiles.TilePeekImage()
                    {
                        Source = sound.ImageFile.Path
                    },
                    Children =
                    {
                        new NotificationsExtensions.AdaptiveText()
                        {
                            Text = sound.Name
                        }
                    },
                    TextStacking = NotificationsExtensions.Tiles.TileTextStacking.Center
                }
            };

            NotificationsExtensions.Tiles.TileContent content = new NotificationsExtensions.Tiles.TileContent()
            {
                Visual = new NotificationsExtensions.Tiles.TileVisual()
                {
                    TileMedium = binding,
                    TileWide = binding,
                    TileLarge = binding
                }
            };

            // Create the tile notification
            var notification = new TileNotification(content.GetXml());
            // And send the notification
            TileUpdateManager.CreateTileUpdaterForApplication().Update(notification);
        }

        public static async Task SetSoundBoardSizeTextAsync()
        {
            if ((App.Current as App)._itemViewHolder.progressRingIsActive)
            {
                await Task.Delay(1000);
                await SetSoundBoardSizeTextAsync();
            }

            float totalSize = 0;
            foreach (Sound sound in (App.Current as App)._itemViewHolder.allSounds)
            {
                float size;
                size = await GetFileSizeInGBAsync(sound.AudioFile);
                if (sound.ImageFile != null)
                {
                    size += await GetFileSizeInGBAsync(sound.ImageFile);
                }
                totalSize += size;
            }

            (App.Current as App)._itemViewHolder.soundboardSize = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("SettingsSoundBoardSize") + totalSize.ToString("n2") + " GB.";
        }
        #endregion

        #region Other Methods
        public static async Task<float> GetFileSizeInGBAsync(StorageFile file)
        {
            BasicProperties pro = await file.GetBasicPropertiesAsync();
            return (((pro.Size / 1024f) / 1024f)/ 1024f);
        }
        
        public static async Task WriteFile(StorageFile file, Object objectToWrite)
        {
            DataContractJsonSerializer js = new DataContractJsonSerializer(objectToWrite.GetType());
            MemoryStream ms = new MemoryStream();
            js.WriteObject(ms, objectToWrite);

            ms.Position = 0;
            StreamReader sr = new StreamReader(ms);
            string data = sr.ReadToEnd();

            await FileIO.WriteTextAsync(file, data);
        }
        
        public static string HTMLEncodeSpecialChars(string text)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in text)
            {
                if (c > 127) // special chars
                    sb.Append(String.Format("&#{0};", (int)c));
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }
        
        public static List<string> GetIconsList()
        {
            List<string> Icons = new List<string>();
            Icons.Add("\uE707");
            Icons.Add("\uE70F");
            Icons.Add("\uE710");
            Icons.Add("\uE711");
            Icons.Add("\uE713");
            Icons.Add("\uE714");
            Icons.Add("\uE715");
            Icons.Add("\uE716");
            Icons.Add("\uE717");
            Icons.Add("\uE718");
            Icons.Add("\uE719");
            Icons.Add("\uE71B");
            Icons.Add("\uE71C");
            Icons.Add("\uE71E");
            Icons.Add("\uE720");
            Icons.Add("\uE722");
            Icons.Add("\uE723");
            Icons.Add("\uE72C");
            Icons.Add("\uE72D");
            Icons.Add("\uE730");
            Icons.Add("\uE734");
            Icons.Add("\uE735");
            Icons.Add("\uE73A");
            Icons.Add("\uE73E");
            Icons.Add("\uE74D");
            Icons.Add("\uE74E");
            Icons.Add("\uE74F");
            Icons.Add("\uE753");
            Icons.Add("\uE765");
            Icons.Add("\uE767");
            Icons.Add("\uE768");
            Icons.Add("\uE769");
            Icons.Add("\uE76E");
            Icons.Add("\uE774");
            Icons.Add("\uE77A");
            Icons.Add("\uE77B");
            Icons.Add("\uE77F");
            Icons.Add("\uE786");
            Icons.Add("\uE7AD");
            Icons.Add("\uE7C1");
            Icons.Add("\uE7C3");
            Icons.Add("\uE7EE");
            Icons.Add("\uE7EF");
            Icons.Add("\uE80F");
            Icons.Add("\uE81D");
            Icons.Add("\uE890");
            Icons.Add("\uE894");
            Icons.Add("\uE895");
            Icons.Add("\uE896");
            Icons.Add("\uE897");
            Icons.Add("\uE899");
            Icons.Add("\uE8AA");
            Icons.Add("\uE8B1");
            Icons.Add("\uE8B8");
            Icons.Add("\uE8BD");
            Icons.Add("\uE8C3");
            Icons.Add("\uE8C6");
            Icons.Add("\uE8C9");
            Icons.Add("\uE8D6");
            Icons.Add("\uE8D7");
            Icons.Add("\uE8E1");
            Icons.Add("\uE8E0");
            Icons.Add("\uE8EA");
            Icons.Add("\uE8EB");
            Icons.Add("\uE8EC");
            Icons.Add("\uE8EF");
            Icons.Add("\uE8F0");
            Icons.Add("\uE8F1");
            Icons.Add("\uE8F3");
            Icons.Add("\uE8FB");
            Icons.Add("\uE909");
            Icons.Add("\uE90A");
            Icons.Add("\uE90B");
            Icons.Add("\uE90F");
            Icons.Add("\uE910");
            Icons.Add("\uE913");

            return Icons;
        }
        #endregion
        
        #region Old Methods
        public static async Task<StorageFolder> CreateDetailsFolderIfNotExistsAsync()
        {
            StorageFolder root = ApplicationData.Current.LocalFolder;
            StorageFolder detailsFolder;
            if (await root.TryGetItemAsync("soundDetails") == null)
            {
                return detailsFolder = await root.CreateFolderAsync("soundDetails");
            }
            else
            {
                return detailsFolder = await root.GetFolderAsync("soundDetails");
            }
        }

        public static async void addImage(StorageFile file, Sound sound)
        {
            (App.Current as App)._itemViewHolder.allSoundsChanged = true;
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFolder imagesFolder = await folder.GetFolderAsync("images");

            if (file.ContentType.Equals("image/png"))
            {
                // Copy new image and delete the old one
                StorageFile newFile = await file.CopyAsync(imagesFolder, sound.Name + ".png", NameCollisionOption.ReplaceExisting);

                StorageFile oldFile = (StorageFile)await imagesFolder.TryGetItemAsync(sound.Name + ".jpg");
                if (oldFile != null)
                {
                    await oldFile.DeleteAsync();
                }
            }
            else if (file.ContentType.Equals("image/jpeg"))
            {
                StorageFile newFile = await file.CopyAsync(imagesFolder, sound.Name + ".jpg", NameCollisionOption.ReplaceExisting);

                StorageFile oldFile = (StorageFile)await imagesFolder.TryGetItemAsync(sound.Name + ".png");
                if (oldFile != null)
                {
                    await oldFile.DeleteAsync();
                }
            }

            // Update GridView
            await UpdateGridView();
        }

        public static async Task renameSound(Sound sound, string newName)
        {
            (App.Current as App)._itemViewHolder.allSoundsChanged = true;
            StorageFile audioFile = sound.AudioFile;
            StorageFile imageFile = sound.ImageFile;
            if (sound.DetailsFile == null)
            {
                sound.DetailsFile = await createSoundDetailsFileIfNotExistsAsync(sound.Name);
            }
            await sound.DetailsFile.RenameAsync(newName + sound.DetailsFile.FileType);

            await audioFile.RenameAsync(newName + audioFile.FileType);
            if (imageFile != null)
            {
                await imageFile.RenameAsync(newName + imageFile.FileType);
            }

            await UpdateGridView();
        }

        public static async Task deleteSound(Sound sound)
        {
            (App.Current as App)._itemViewHolder.allSoundsChanged = true;
            await sound.AudioFile.DeleteAsync();
            if (sound.ImageFile != null)
            {
                await sound.ImageFile.DeleteAsync();
            }
            if (sound.DetailsFile == null)
            {
                await createSoundDetailsFileIfNotExistsAsync(sound.Name);
            }
            await sound.DetailsFile.DeleteAsync();
        }

        public static async Task setSoundAsFavourite(Sound sound, bool favourite)
        {
            // Check if details file of the sound exists
            if (sound.DetailsFile == null)
            {
                sound.DetailsFile = await createSoundDetailsFileIfNotExistsAsync(sound.Name);
            }

            // Create new details object and write to details file
            SoundDetails details = new SoundDetails
            {
                Category = sound.Category.Name,
                Favourite = favourite
            };
            await WriteFile(sound.DetailsFile, details);
        }

        public static async Task<StorageFile> createDataFolderAndJsonFileIfNotExistsAsync()
        {
            StorageFolder root = ApplicationData.Current.LocalFolder;
            StorageFolder dataFolder;
            if (await root.TryGetItemAsync("data") == null)
            {
                dataFolder = await root.CreateFolderAsync("data");
            }
            else
            {
                dataFolder = await root.GetFolderAsync("data");
            }

            StorageFile dataFile;
            if (await dataFolder.TryGetItemAsync("data.json") == null)
            {
                dataFile = await dataFolder.CreateFileAsync("data.json");
                await FileIO.WriteTextAsync(dataFile, "{\"Categories\": []}");
            }
            else
            {
                dataFile = await dataFolder.GetFileAsync("data.json");
            }
            return dataFile;
        }

        public static async Task deleteExportAndImportFoldersAsync()
        {
            StorageFolder localDataFolder = ApplicationData.Current.LocalFolder;

            if (await localDataFolder.TryGetItemAsync("export") != null)
            {
                await (await localDataFolder.GetFolderAsync("export")).DeleteAsync();
            }

            if (await localDataFolder.TryGetItemAsync("import") != null)
            {
                await (await localDataFolder.GetFolderAsync("import")).DeleteAsync();
            }


            if (await localDataFolder.TryGetItemAsync("import.zip") != null)
            {
                await (await localDataFolder.GetFileAsync("import.zip")).DeleteAsync();
            }

            if (await localDataFolder.TryGetItemAsync("export.zip") != null)
            {
                await (await localDataFolder.GetFileAsync("export.zip")).DeleteAsync();
            }
        }

        public static async Task<ObservableCollection<Category>> GetCategoriesListAsync()
        {
            StorageFile dataFile = await FileManager.createDataFolderAndJsonFileIfNotExistsAsync();
            string data = await FileIO.ReadTextAsync(dataFile);

            //Deserialize Json
            var serializer = new DataContractJsonSerializer(typeof(Data));
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(data));
            var dataReader = (Data)serializer.ReadObject(ms);

            ObservableCollection<Category> categoriesList = dataReader.Categories;
            foreach (Category category in categoriesList)
            {
                category.Name = WebUtility.HtmlDecode(category.Name);
            }

            return categoriesList;
        }

        public static async Task SaveCategoriesListAsync(ObservableCollection<Category> categories)
        {
            StorageFile dataFile = await FileManager.createDataFolderAndJsonFileIfNotExistsAsync();

            Data data = new Data();
            data.Categories = categories;

            foreach (var category in data.Categories)
            {
                category.Name = HTMLEncodeSpecialChars(category.Name);
            }

            DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(Data));
            MemoryStream ms = new MemoryStream();
            js.WriteObject(ms, data);

            ms.Position = 0;
            StreamReader sr = new StreamReader(ms);
            string dataString = sr.ReadToEnd();

            await FileIO.WriteTextAsync(dataFile, dataString);

            await GetCategoriesListAsync();
        }

        public async static Task renameCategory(string oldName, string newName)
        {
            (App.Current as App)._itemViewHolder.allSoundsChanged = true;
            foreach (var sound in (App.Current as App)._itemViewHolder.sounds)
            {
                if (sound.Category.Name == oldName)
                {
                    SoundDetails details = new SoundDetails();
                    details.Category = newName;
                    details.Favourite = sound.Favourite;
                    await WriteFile(sound.DetailsFile, details);
                }
            }
        }

        public static async Task deleteCategory(string name)
        {
            ObservableCollection<Category> categories = await GetCategoriesListAsync();

            Category deletedCategory = new Category();
            foreach (Category category in categories)
            {
                if (category.Name == name)
                {
                    deletedCategory = category;
                }
            }

            categories.Remove(deletedCategory);

            await SaveCategoriesListAsync(categories);
        }

        public static async Task ExportData(StorageFolder destinationFolder)
        {
            var stringLoader = new Windows.ApplicationModel.Resources.ResourceLoader();
            (App.Current as App)._itemViewHolder.exported = false;
            (App.Current as App)._itemViewHolder.imported = false;
            (App.Current as App)._itemViewHolder.isExporting = true;
            (App.Current as App)._itemViewHolder.areExportAndImportButtonsEnabled = false;
            (App.Current as App)._itemViewHolder.exportMessage = stringLoader.GetString("ExportMessage-1"); // 1

            await deleteExportAndImportFoldersAsync();
            await createDataFolderAndJsonFileIfNotExistsAsync();
            await CreateDetailsFolderIfNotExistsAsync();

            // Copy all data into the folder
            await FileManager.GetAllSounds();

            // Create folders in export folder
            await CreateExportFoldersAsync();

            StorageFolder localDataFolder = ApplicationData.Current.LocalFolder;
            StorageFolder exportFolder = await localDataFolder.GetFolderAsync("export");
            StorageFolder imagesExportFolder = await exportFolder.GetFolderAsync("images");
            StorageFolder soundDetailsExportFolder = await exportFolder.GetFolderAsync("soundDetails");
            StorageFolder dataFolder = await localDataFolder.GetFolderAsync("data");
            StorageFolder dataExportFolder = await exportFolder.GetFolderAsync("data");
            StorageFile dataFile = await dataFolder.GetFileAsync("data.json");

            (App.Current as App)._itemViewHolder.exportMessage = stringLoader.GetString("ExportMessage-2"); // 2

            // Copy the files into the export folder
            foreach (Sound sound in (App.Current as App)._itemViewHolder.allSounds)
            {
                await sound.AudioFile.CopyAsync(exportFolder, sound.AudioFile.Name, NameCollisionOption.ReplaceExisting);
                await sound.DetailsFile.CopyAsync(soundDetailsExportFolder, sound.DetailsFile.Name, NameCollisionOption.ReplaceExisting);
                if (sound.ImageFile != null)
                {
                    await sound.ImageFile.CopyAsync(imagesExportFolder, sound.ImageFile.Name, NameCollisionOption.ReplaceExisting);
                }
            }
            await dataFile.CopyAsync(dataExportFolder, dataFile.Name, NameCollisionOption.ReplaceExisting);
            (App.Current as App)._itemViewHolder.exportMessage = stringLoader.GetString("ExportMessage-3"); // 3

            // Create Zip file in local storage
            IAsyncAction asyncAction = Windows.System.Threading.ThreadPool.RunAsync(
                    async (workItem) =>
                    {
                        var t = Task.Run(() => ZipFile.CreateFromDirectory(exportFolder.Path, localDataFolder.Path + @"\export.zip"));
                        t.Wait();

                        // Get the created file and move it to the picked folder
                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                            CoreDispatcherPriority.High,
                            new DispatchedHandler(() =>
                            {
                                (App.Current as App)._itemViewHolder.exportMessage = stringLoader.GetString("ExportMessage-4"); // 4
                            }));

                        StorageFile exportZipFile = await localDataFolder.GetFileAsync("export.zip");
                        await exportZipFile.MoveAsync(destinationFolder, "UniversalSoundBoard " + DateTime.Today.ToString("dd.MM.yyyy") + ".zip", NameCollisionOption.GenerateUniqueName);

                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                            CoreDispatcherPriority.High,
                            new DispatchedHandler(async () =>
                            {
                                (App.Current as App)._itemViewHolder.exportMessage = stringLoader.GetString("ExportImportMessage-TidyUp"); // TidyUp
                                await deleteExportAndImportFoldersAsync();

                                (App.Current as App)._itemViewHolder.exportMessage = "";
                                (App.Current as App)._itemViewHolder.isExporting = false;
                                (App.Current as App)._itemViewHolder.exported = true;
                                (App.Current as App)._itemViewHolder.areExportAndImportButtonsEnabled = true;
                            }));
                        // SendExportSuccessfullNotification();
                    });
        }

        public static async Task ImportDataZip(StorageFile zipFile)
        {
            var stringLoader = new Windows.ApplicationModel.Resources.ResourceLoader();
            (App.Current as App)._itemViewHolder.isImporting = true;
            (App.Current as App)._itemViewHolder.exported = false;
            (App.Current as App)._itemViewHolder.imported = false;
            (App.Current as App)._itemViewHolder.areExportAndImportButtonsEnabled = false;
            (App.Current as App)._itemViewHolder.importMessage = stringLoader.GetString("ImportMessage-1"); // 1

            await deleteExportAndImportFoldersAsync();
            await CreateImportFolders();

            StorageFolder localDataFolder = ApplicationData.Current.LocalFolder;
            StorageFolder importFolder = await localDataFolder.GetFolderAsync("import");

            // Copy zip file into local storage
            StorageFile newZipFile = await zipFile.CopyAsync(localDataFolder, "import.zip", NameCollisionOption.ReplaceExisting);

            (App.Current as App)._itemViewHolder.importMessage = stringLoader.GetString("ImportMessage-2"); // 2

            IAsyncAction asyncAction = Windows.System.Threading.ThreadPool.RunAsync(
                    async (workItem) =>
                    {
                        // Extract zip file in local storage
                        var t = Task.Run(() => ZipFile.ExtractToDirectory(newZipFile.Path, importFolder.Path));
                        t.Wait();

                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                            CoreDispatcherPriority.High,
                            new DispatchedHandler(() =>
                            {

                                (App.Current as App)._itemViewHolder.importMessage = stringLoader.GetString("ImportMessage-3"); // 3
                            }));

                        await ImportData();

                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                            CoreDispatcherPriority.High,
                            new DispatchedHandler(async () =>
                            {
                                (App.Current as App)._itemViewHolder.importMessage = stringLoader.GetString("ExportImportMessage-TidyUp"); // TidyUp
                                await deleteExportAndImportFoldersAsync();

                                (App.Current as App)._itemViewHolder.importMessage = "";
                                (App.Current as App)._itemViewHolder.isImporting = false;
                                (App.Current as App)._itemViewHolder.imported = true;
                                (App.Current as App)._itemViewHolder.allSoundsChanged = true;
                                (App.Current as App)._itemViewHolder.areExportAndImportButtonsEnabled = true;

                                await FileManager.GetAllSounds();
                                CreateCategoriesObservableCollection();
                                await SetSoundBoardSizeTextAsync();
                            }));
                    });
        }

        private async static Task ImportData()
        {
            StorageFolder localDataFolder = ApplicationData.Current.LocalFolder;
            StorageFolder importFolder = await localDataFolder.GetFolderAsync("import");
            StorageFolder soundDetailsImportFolder = await importFolder.GetFolderAsync("soundDetails");
            StorageFolder imagesImportFolder = await importFolder.GetFolderAsync("images");
            StorageFolder dataImportFolder = await importFolder.GetFolderAsync("data");

            StorageFolder soundDetailsFolder = await localDataFolder.GetFolderAsync("soundDetails");
            StorageFolder imagesFolder = await localDataFolder.GetFolderAsync("images");

            // Copy Sound files into local storage
            foreach (var file in await importFolder.GetFilesAsync())
            {
                if (file.ContentType == "audio/wav" || file.ContentType == "audio/mpeg")
                {
                    if (await localDataFolder.TryGetItemAsync(file.Name) == null)
                    {
                        await file.CopyAsync(localDataFolder);
                    }
                }
            }

            // Copy detail files into local storage
            foreach (var file in await soundDetailsImportFolder.GetFilesAsync())
            {
                if (await soundDetailsFolder.TryGetItemAsync(file.Name) == null)
                {
                    await file.CopyAsync(soundDetailsFolder);
                }
            }

            // Copy images into local storage
            foreach (var file in await imagesImportFolder.GetFilesAsync())
            {
                if (file.ContentType == "image/jpeg" || file.ContentType == "image/png")
                {
                    if (await imagesFolder.TryGetItemAsync(file.Name) == null)
                    {
                        await file.CopyAsync(imagesFolder);
                    }
                }
            }

            // Read data.json and add categories
            StorageFile dataFile = await createDataFolderAndJsonFileIfNotExistsAsync();
            StorageFile dataImportFile = await dataImportFolder.GetFileAsync("data.json");

            // Read data file
            string data = await FileIO.ReadTextAsync(dataFile);
            var serializer = new DataContractJsonSerializer(typeof(Data));
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(data));
            var dataReader = (Data)serializer.ReadObject(ms);
            ObservableCollection<Category> categoriesList = dataReader.Categories;

            // Read import data file
            string importData = await FileIO.ReadTextAsync(dataImportFile);
            var serializer2 = new DataContractJsonSerializer(typeof(Data));
            var ms2 = new MemoryStream(Encoding.UTF8.GetBytes(importData));
            var dataReader2 = (Data)serializer.ReadObject(ms2);
            ObservableCollection<Category> importCategoriesList = dataReader2.Categories;

            // Add imported categories to original list
            foreach (Category category in importCategoriesList)
            {
                if (categoriesList.Where(cat => cat.Name.Equals(category.Name)).Count() == 0)
                {
                    categoriesList.Add(category);
                }
            }

            // Write data.json with new categories list
            Data categoryData = new Data();
            categoryData.Categories = categoriesList;
            await WriteFile(dataFile, categoryData);

            await GetCategoriesListAsync();
        }

        private static async Task CreateExportFoldersAsync()
        {
            StorageFolder localDataFolder = ApplicationData.Current.LocalFolder;

            StorageFolder exportFolder;
            if (await localDataFolder.TryGetItemAsync("export") == null)
            {
                exportFolder = await localDataFolder.CreateFolderAsync("export");
            }
            else
            {
                exportFolder = await localDataFolder.GetFolderAsync("export");
            }

            if (await exportFolder.TryGetItemAsync("images") == null)
            {
                await exportFolder.CreateFolderAsync("images");
            }

            if (await exportFolder.TryGetItemAsync("soundDetails") == null)
            {
                await exportFolder.CreateFolderAsync("soundDetails");
            }

            if (await exportFolder.TryGetItemAsync("data") == null)
            {
                await exportFolder.CreateFolderAsync("data");
            }
        }

        private async static Task CreateImportFolders()
        {
            StorageFolder localDataFolder = ApplicationData.Current.LocalFolder;

            StorageFolder importFolder;
            if (await localDataFolder.TryGetItemAsync("import") == null)
            {
                importFolder = await localDataFolder.CreateFolderAsync("import");
            }
            else
            {
                importFolder = await localDataFolder.GetFolderAsync("import");
            }
        }

        public static async Task<Category> GetCategoryByNameAsync(string categoryName)
        {
            if (categoryName != "" || (await GetCategoriesListAsync()).Count >= 1)
            {
                ObservableCollection<Category> categories = await GetCategoriesListAsync();
                foreach (Category category in categories)
                {
                    if (category.Name == categoryName)
                    {
                        return category;
                    }
                }
            }
            return null;
        }

        public static async Task<StorageFile> createSoundDetailsFileIfNotExistsAsync(string soundName)
        {
            StorageFolder detailsFolder = await CreateDetailsFolderIfNotExistsAsync();
            StorageFile detailsFile;
            if (await detailsFolder.TryGetItemAsync(soundName + ".json") == null)
            {
                // Create file and write empty json
                detailsFile = await detailsFolder.CreateFileAsync(soundName + ".json");
                SoundDetails details = new SoundDetails();
                details.Category = "";

                await WriteFile(detailsFile, details);

                return detailsFile;
            }
            else
            {
                return detailsFile = await detailsFolder.GetFileAsync(soundName + ".json");
            }
        }

        public static async Task addSound(Sound sound)
        {
            (App.Current as App)._itemViewHolder.allSoundsChanged = true;
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFile newFile = await sound.AudioFile.CopyAsync(folder, sound.AudioFile.Name, NameCollisionOption.GenerateUniqueName);
            await createSoundDetailsFileIfNotExistsAsync(sound.Name);
            if (sound.Category != null)
            {
                sound.SetCategory(sound.Category);
            }
        }
        #endregion
        
    }
}
