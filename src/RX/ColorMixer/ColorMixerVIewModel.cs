using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ColorMixer.Annotations;
using ColorMixerLib;

namespace ColorMixer
{
    public class ColorMixerVIewModel : INotifyPropertyChanged
    {
        private Selector selector;
        public ColorMixerVIewModel(Slider red, Slider green, Slider blue)
        {
            CurrentColor = "Click me";
            selector = new Selector(red, green, blue);
            clickMeCommand=new ClickMeCommand(selector);
            selector.ColorChanged += selector_ColorChanged;
            selector.CurrentColor += selector_CurrentColor;
        }

        void selector_CurrentColor(object sender, Color args)
        {
            CurrentColor = String.Format("R {0} - G {1} - B {2}", args.R, args.G, args.B);
        }

        void selector_ColorChanged(object sender, SolidColorBrush args)
        {
            ColorMixed = args;
        }

        private string currentColor;
        public string CurrentColor
        {
        get { return currentColor; }
            set
            {
                currentColor = value;
                OnPropertyChanged();
            }
        }
        

        private SolidColorBrush _colorMixed;
        public SolidColorBrush ColorMixed
        {
            get { return _colorMixed; }
            set { _colorMixed = value; OnPropertyChanged(); }
        }

        private ICommand clickMeCommand;

        public ICommand ClickMeCommand
        {
            get
            {
                return clickMeCommand;
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    internal class ClickMeCommand : ICommand
    {
        private readonly Selector _selector;

        public ClickMeCommand(Selector selector)
        {
            _selector = selector;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            _selector.GetColor();
        }

        public event EventHandler CanExecuteChanged;
    }
}
