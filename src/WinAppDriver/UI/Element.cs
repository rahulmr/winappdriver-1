﻿namespace WinAppDriver.UI
{
    using System.Windows;
    using System.Windows.Automation;

    internal class Element : IElement
    {
        private AutomationElement element;

        private AutomationElement.AutomationElementInformation? infoCache;

        private Rect? rectCache;

        public Element(AutomationElement element)
        {
            this.element = element;
        }

        public string ID
        {
            get { return this.Info.AutomationId; }
        }

        public string TypeName
        {
            get
            {
                // ControlType.[TypeName]
                return this.Info.ControlType.ProgrammaticName.Substring(12);
            }
        }

        public string Name
        {
            get { return this.Info.Name; }
        }

        public string ClassName
        {
            get { return this.Info.ClassName; }
        }

        public bool Visible
        {
            get { return !this.Info.IsOffscreen; }
        }

        public bool Enabled
        {
            get { return this.Info.IsEnabled; }
        }

        public bool Selected
        {
            get
            {
                object pattern;
                if (this.element.TryGetCurrentPattern(TogglePattern.Pattern, out pattern))
                {
                    var state = ((TogglePattern)pattern).Current.ToggleState;
                    return state != ToggleState.Off;
                }
                else
                {
                    return false;
                }
            }
        }

        public int X
        {
            get { return (int)this.Rect.X; }
        }

        public int Y
        {
            get { return (int)this.Rect.Y; }
        }

        public int Width
        {
            get { return (int)this.Rect.Width; }
        }

        public int Height
        {
            get { return (int)this.Rect.Height; }
        }

        private AutomationElement.AutomationElementInformation Info
        {
            get
            {
                if (this.infoCache == null)
                {
                    this.infoCache = this.element.Current;
                }

                return this.infoCache.Value;
            }
        }

        private Rect Rect
        {
            get
            {
                if (this.rectCache == null)
                {
                    this.rectCache = this.Info.BoundingRectangle;
                }

                return this.rectCache.Value;
            }
        }
    }
}