﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UniversalSoundBoard.Model;
using Windows.Media.Playback;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundBoard
{
    public class ItemViewHolder : INotifyPropertyChanged
    {
        private string _title;
        private bool _progressRingIsActive;
        private bool _multiSelectOptionsEnabled;
        private string _searchQuery;
        private Visibility _editButtonVisibility;
        private Visibility _playAllButtonVisibility;
        private bool _normalOptionsVisibility;
        private Type _page;
        private ListViewSelectionMode _selectionMode;
        private ObservableCollection<Category> _categories;
        private ObservableCollection<Sound> _sounds;
        private ObservableCollection<Sound> _favouriteSounds;
        private ObservableCollection<Sound> _allSounds;
        private bool _allSoundsChanged;
        private List<Sound> _selectedSounds;
        private ObservableCollection<PlayingSound> _playingSounds;
        private Visibility _playingSoundsListVisibility;
        private bool _playOneSoundAtOnce;
        private bool _showCategoryIcon;
        private bool _showSoundsPivot;
        private bool _isExporting;
        private bool _exported;
        private bool _isImporting;
        private bool _imported;
        private bool _areExportAndImportButtonsEnabled;
        private string _exportMessage;
        private string _importMessage;
        private string _soundboardSize;
        private Thickness _windowTitleMargin;
        private bool _searchAutoSuggestBoxVisibility;
        private bool _volumeButtonVisibility;
        private bool _addButtonVisibility;
        private bool _selectButtonVisibility;
        private bool _searchButtonVisibility;
        private bool _cancelButtonVisibility;
        private bool _moreButtonVisibility;
        private bool _topButtonsCollapsed;

        public string title
        {
            get { return _title; }

            set
            {
                _title = value;
                NotifyPropertyChanged("title");
            }
        }

        public bool progressRingIsActive
        {
            get { return _progressRingIsActive; }

            set
            {
                _progressRingIsActive = value;
                NotifyPropertyChanged("progressRingIsActive");
            }
        }

        public bool multiSelectOptionsEnabled
        {
            get { return _multiSelectOptionsEnabled; }

            set
            {
                _multiSelectOptionsEnabled = value;
                NotifyPropertyChanged("multiSelectOptionsEnabled");
            }
        }

        public ObservableCollection<Sound> sounds
        {
            get { return _sounds; }

            set
            {
                _sounds = value;
                NotifyPropertyChanged("sounds");
            }
        }

        public ObservableCollection<Sound> favouriteSounds
        {
            get { return _favouriteSounds; }

            set
            {
                _favouriteSounds = value;
                NotifyPropertyChanged("favouriteSounds");
            }
        }

        public ObservableCollection<Sound> allSounds
        {
            get { return _allSounds; }

            set
            {
                _allSounds = value;
                NotifyPropertyChanged("allSounds");
            }
        }

        public bool allSoundsChanged
        {
            get { return _allSoundsChanged; }

            set
            {
                _allSoundsChanged = value;
                NotifyPropertyChanged("allSoundsChanged");
            }
        }

        public ObservableCollection<Category> categories
        {
            get { return _categories; }

            set
            {
                _categories = value;
                NotifyPropertyChanged("categories");
            }
        }

        public string searchQuery
        {
            get { return _searchQuery; }

            set
            {
                _searchQuery = value;
                NotifyPropertyChanged("searchQuery");
            }
        }

        public Visibility editButtonVisibility
        {
            get { return _editButtonVisibility; }

            set
            {
                _editButtonVisibility = value;
                NotifyPropertyChanged("editButtonVisibility");
            }
        }

        public Visibility playAllButtonVisibility
        {
            get { return _playAllButtonVisibility; }

            set
            {
                _playAllButtonVisibility = value;
                NotifyPropertyChanged("playAllButtonVisibility");
            }
        }

        public bool normalOptionsVisibility
        {
            get { return _normalOptionsVisibility; }

            set
            {
                _normalOptionsVisibility = value;
                NotifyPropertyChanged("normalOptionsVisibility");
            }
        }

        public Type page
        {
            get { return _page; }

            set
            {
                _page = value;
                NotifyPropertyChanged("page");
            }
        }

        public ListViewSelectionMode selectionMode
        {
            get { return _selectionMode; }

            set
            {
                _selectionMode = value;
                NotifyPropertyChanged("selectionMode");
            }
        }

        public List<Sound> selectedSounds
        {
            get { return _selectedSounds; }

            set
            {
                _selectedSounds = value;
                NotifyPropertyChanged("selectedSounds");
            }
        }

        public ObservableCollection<PlayingSound> playingSounds
        {
            get { return _playingSounds; }

            set
            {
                _playingSounds = value;
                NotifyPropertyChanged("playingSounds");
            }
        }

        public Visibility playingSoundsListVisibility
        {
            get { return _playingSoundsListVisibility; }

            set
            {
                _playingSoundsListVisibility = value;
                NotifyPropertyChanged("playingSoundsListVisibility");
            }
        }

        public bool playOneSoundAtOnce
        {
            get { return _playOneSoundAtOnce; }

            set
            {
                _playOneSoundAtOnce = value;
                NotifyPropertyChanged("playOneSoundAtOnce");
            }
        }

        public bool showCategoryIcon
        {
            get { return _showCategoryIcon; }

            set
            {
                _showCategoryIcon = value;
                NotifyPropertyChanged("showCategoryIcon");
            }
        }

        public bool showSoundsPivot
        {
            get { return _showSoundsPivot; }

            set
            {
                _showSoundsPivot = value;
                NotifyPropertyChanged("showSoundsPivot");
            }
        }

        public bool isExporting
        {
            get { return _isExporting; }

            set
            {
                _isExporting = value;
                NotifyPropertyChanged("isExporting");
            }
        }

        public bool exported
        {
            get { return _exported; }

            set
            {
                _exported = value;
                NotifyPropertyChanged("exported");
            }
        }

        public bool isImporting
        {
            get { return _isImporting; }

            set
            {
                _isImporting = value;
                NotifyPropertyChanged("isImporting");
            }
        }

        public bool imported
        {
            get { return _imported; }

            set
            {
                _imported = value;
                NotifyPropertyChanged("imported");
            }
        }

        public bool areExportAndImportButtonsEnabled
        {
            get { return _areExportAndImportButtonsEnabled; }

            set
            {
                _areExportAndImportButtonsEnabled = value;
                NotifyPropertyChanged("areExportAndImportButtonsEnabled");
            }
        }

        public string exportMessage
        {
            get { return _exportMessage; }

            set
            {
                _exportMessage = value;
                NotifyPropertyChanged("exportMessage");
            }
        }

        public string importMessage
        {
            get { return _importMessage; }

            set
            {
                _importMessage = value;
                NotifyPropertyChanged("importMessage");
            }
        }

        public string soundboardSize
        {
            get { return _soundboardSize; }

            set
            {
                _soundboardSize = value;
                NotifyPropertyChanged("soundboardSize");
            }
        }

        public Thickness windowTitleMargin
        {
            get { return _windowTitleMargin; }

            set
            {
                _windowTitleMargin = value;
                NotifyPropertyChanged("windowTitleMargin");
            }
        }

        public bool searchAutoSuggestBoxVisibility
        {
            get { return _searchAutoSuggestBoxVisibility; }

            set
            {
                _searchAutoSuggestBoxVisibility = value;
                NotifyPropertyChanged("searchAutoSuggestBoxVisibility");
            }
        }

        public bool volumeButtonVisibility
        {
            get { return _volumeButtonVisibility; }

            set
            {
                _volumeButtonVisibility = value;
                NotifyPropertyChanged("volumeButtonVisibility");
            }
        }

        public bool addButtonVisibility
        {
            get { return _addButtonVisibility; }

            set
            {
                _addButtonVisibility = value;
                NotifyPropertyChanged("addButtonVisibility");
            }
        }

        public bool selectButtonVisibility
        {
            get { return _selectButtonVisibility; }

            set
            {
                _selectButtonVisibility = value;
                NotifyPropertyChanged("selectButtonVisibility");
            }
        }

        public bool searchButtonVisibility
        {
            get { return _searchButtonVisibility; }

            set
            {
                _searchButtonVisibility = value;
                NotifyPropertyChanged("searchButtonVisibility");
            }
        }

        public bool cancelButtonVisibility
        {
            get { return _cancelButtonVisibility; }

            set
            {
                _cancelButtonVisibility = value;
                NotifyPropertyChanged("cancelButtonVisibility");
            }
        }

        public bool moreButtonVisibility
        {
            get { return _moreButtonVisibility; }

            set
            {
                _moreButtonVisibility = value;
                NotifyPropertyChanged("moreButtonVisibility");
            }
        }

        public bool topButtonsCollapsed
        {
            get { return _topButtonsCollapsed; }

            set
            {
                _topButtonsCollapsed = value;
                NotifyPropertyChanged("topButtonsCollapsed");
            }
        }



        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (object.Equals(storage, value)) return false;
            storage = value;
            this.OnPropertyChanged(propertyName);
            return true;
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            var eventHandler = this.PropertyChanged;
            if (eventHandler != null)
                eventHandler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
