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

		Image image = null;
		String imageSource = null;
		String imageSourceFile = null;
		Label label = null;

		public byte[] Hash;

		public String ImageName { 
			set
			{
				if (value != null)
				{
					image = new Image();

					imageSource = value;
					image.Pixbuf = Utils.ToPixbuf(Constants.Images.Button(value, false));

					vbox1.PackEnd(image, true, true, 0);
					image.Show();
				}
			}
		}

		public String ImageFileName
		{
			set
			{
				if (value != null && System.IO.File.Exists(value))
				{
					image = new Image();

					imageSourceFile = value;
					image.Pixbuf = new Gdk.Pixbuf(value);

					vbox1.PackEnd(image, true, true, 0);
					image.Show();
				}
			}
		}

		public String Caption { 
			set 
			{ 
				label = new Label ();

				label.Text = value;

				vbox1.PackStart(label, true, true, 0);
				//else bold or not
				label.Show();
			}
		}

		public bool Selected { 
			set 
			{
				if (image != null) {
					try {
						if (imageSource != null)
						{
							image.Pixbuf = Gdk.Pixbuf.LoadFromResource(Constants.Images.Button(imageSource, value));
						}
						else if (imageSourceFile != null)
						{
							image.Pixbuf = new Gdk.Pixbuf(imageSourceFile);
							image.Pixbuf = image.Pixbuf.ScaleSimple(64, 64, Gdk.InterpType.Hyper);
						}
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