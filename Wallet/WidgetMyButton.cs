using System;

namespace Wallet
{
	[System.ComponentModel.ToolboxItem (true)]
	public partial class WidgetMyButton : Gtk.Bin
	{
		public WidgetMyButton ()
		{
			this.Build ();

			Selected = false;

			eventbox9.ButtonPressEvent += delegate {
				Select();
				Menu.Selection = Name;
			};
		}

		public void Select() {
			foreach (Gtk.Widget widget in ((Gtk.Container)Parent).Children) {
				if (widget is WidgetMyButton) {
					((WidgetMyButton)widget).Selected = widget.Name == Name;
				}
			}
		}
			
		public bool Selected { 
			set 
			{
				eventbox9.ModifyBg(Gtk.StateType.Normal, !value ? new Gdk.Color(0x01d,0x025,0x030) : new Gdk.Color(0x028,0x02f,0x037));
				WidgetButtonContent WidgetButtonContent = GetWidgetButtonContent ();

				String asset = "Wallet.Assets." + Name + (value ? "_on.png" : "_off.png");

				try {
					WidgetButtonContent.SetBackground(asset);
				} catch {
					Console.WriteLine("missing" + asset);
				}
			}
		}

		private WidgetButtonContent GetWidgetButtonContent() {
			Gtk.Container c = (Gtk.Container) Children[0];
			c = (Gtk.Container) c.Children [0];	
			c = (Gtk.Container) c.Children [0];	

			return (WidgetButtonContent)c.Children [0];
		}

		private IMenu Menu { 
			get {
				return (IMenu)Parent.Parent;
			}
		}
	}
}

