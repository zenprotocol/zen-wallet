using System;
using Gtk;

namespace Wallet
{
	[System.ComponentModel.ToolboxItem (true)]
	public partial class MenuButton : WidgetBase
	{
		public MenuButton ()
		{
			this.Build ();

			Selected = false;

			ButtonPressEvent (eventbox, () => {
				Select();
				FindParent<MenuBase>().Selection = this;
			});
		}

		public void Select() {
			foreach (Widget widget in FindParent<Container>().Children) {
				if (widget is MenuButton) {
					((MenuButton)widget).Selected = this == widget;
				}
			}
		}

		private Image image = null;
		private String imageSource = null;
		private Label label = null;

		public String ImageName { 
			set 
			{ 
				image = new Image ();

				imageSource = value;
				image.Pixbuf = Utils.ToPixbuf(Constants.Images.Button(value, false));

				hbox1.PackStart(image, true, true, 0);
			}
		}

		public String Caption { 
			set 
			{ 
				label = new Label ();

				label.Text = value;

				hbox1.PackStart(label, true, true, 0);
				 //else bold or not
			}
		}

		public bool Selected { 
			set 
			{
				if (image != null) {
					try {
						image.Pixbuf = Gdk.Pixbuf.LoadFromResource (Constants.Images.Button (imageSource, value));
					} catch {
						Console.WriteLine ("missing " + Constants.Images.Button (imageSource, value));
					}
				}

				if (label != null) {
					label.ModifyFg(Gtk.StateType.Normal, value ? Constants.Colors.Text.Gdk : Constants.Colors.SubText.Gdk);
				}

				eventbox.ModifyBg(Gtk.StateType.Normal, value ? Constants.Colors.ButtonSelected.Gdk : Constants.Colors.ButtonUnselected.Gdk);
			}
		}
	}
}