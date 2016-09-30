using System;

namespace Wallet
{
	[System.ComponentModel.ToolboxItem (true)]
	public partial class MenuButton : WidgetBase
	{
		public MenuButton ()
		{
			this.Build ();

			Selected = false;

			eventbox9.ButtonPressEvent += delegate {
				Select();
				FindParent<MenuBase>().Selection = Name;
			};
		}

		public void Select() {
			foreach (Gtk.Widget widget in ((Gtk.Container)Parent).Children) {
				if (widget is MenuButton) {
					((MenuButton)widget).Selected = widget.Name == Name;
				}
			}
		}
			
		public bool Selected { 
			set 
			{
				eventbox9.ModifyBg(Gtk.StateType.Normal, !value ? new Gdk.Color(0x01d,0x025,0x030) : new Gdk.Color(0x028,0x02f,0x037));

				String asset = "Wallet.Assets." + Name + (value ? "_on.png" : "_off.png");

				try {
					FindChild<ImageButton>().SetBackground(asset);
				} catch {
					Console.WriteLine("missing" + asset);
				}
			}
		}
	}
}

