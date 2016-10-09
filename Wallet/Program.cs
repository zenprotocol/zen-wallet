using System;
using Gtk;
using System.Collections;

namespace Wallet
{
	public class Song
	{
		public Song (string artist, string title)
		{
			this.Artist = artist;
			this.Title = title;
		}

		public string Artist;
		public string Title;
	}

	class Program
	{

		static HScale hscale;
		static VScale vscale;

		static void scale_set_default_values (Scale s)
		{
			s.UpdatePolicy = UpdateType.Continuous;
			s.Digits = 1;
			s.ValuePos = PositionType.Top;
			s.DrawValue = true;
		}

		static void create_range_controls ()
		{
			Window window;
			VBox box1, box3;
			Box box2;
	
			HScrollbar scrollbar;

			window = new Window (WindowType.Toplevel);
			window.Title = "range controls";

			box1 = new VBox (false, 0);
			window.Add (box1);
			box1.ShowAll ();

			box2 = new HBox (false, 0);
			box2.BorderWidth = 10;
			box1.PackStart (box2, true, true, 0);
			box2.ShowAll ();

			/* value, lower, upper, step_increment, page_increment, page_size */
			/* Note that the page_size value only makes a difference for
			* scrollbar widgets, and the highest value you'll get is actually
			* (upper - page_size). */
			Adjustment adj1 = new Adjustment (0.0, 0.0, 101.0, 0.1, 1.0, 1.0);

			vscale = new VScale ((Adjustment) adj1);
			scale_set_default_values (vscale);

			box2.PackStart (vscale, true, true, 0);
			vscale.ShowAll ();

			box3 = new VBox (false, 10);
			box2.PackStart (box3, true, true, 0);
			box3.ShowAll ();

			/* Reuse the same adjustment */
			hscale = new HScale ((Adjustment) adj1);
			hscale.SetSizeRequest (200, -1);
		//	scale_set_default_values (hscale);

			box3.PackStart (hscale, true, true, 0);
			hscale.ShowAll ();

			/* reuse the same adjustment again */
			scrollbar = new HScrollbar ((Adjustment) adj1);

			/* Notice how this causes the scales to always be updated
                         * continuously when the scrollbar is moved */
			scrollbar.UpdatePolicy = UpdateType.Continuous;

			box3.PackStart (scrollbar, true, true, 0);
			scrollbar.ShowAll ();

	






			ArrayList songs = new ArrayList ();

			songs.Add (new Song ("Dancing DJs vs. Roxette", "Fading Like a Flower"));
			songs.Add (new Song ("Xaiver", "Give me the night"));
			songs.Add (new Song ("Daft Punk", "Technologic"));
			songs.Add (new Song ("Dancing DJs vs. Roxette", "Fading Like a Flower"));
			songs.Add (new Song ("Xaiver", "Give me the night"));
			songs.Add (new Song ("Daft Punk", "Technologic"));
			songs.Add (new Song ("Dancing DJs vs. Roxette", "Fading Like a Flower"));
			songs.Add (new Song ("Xaiver", "Give me the night"));
			songs.Add (new Song ("Daft Punk", "Technologic"));
			songs.Add (new Song ("Dancing DJs vs. Roxette", "Fading Like a Flower"));
			songs.Add (new Song ("Xaiver", "Give me the night"));
			songs.Add (new Song ("Daft Punk", "Technologic"));

			Gtk.TreeView tree = new Gtk.TreeView ();


			Gtk.TreeViewColumn artistColumn = new Gtk.TreeViewColumn ();
			artistColumn.Title = "Artist";
			Gtk.CellRendererText artistNameCell = new Gtk.CellRendererText ();
			artistColumn.PackStart (artistNameCell, true);

			Gtk.TreeViewColumn songColumn = new Gtk.TreeViewColumn ();
			songColumn.Title = "Song Title";
			Gtk.CellRendererText songTitleCell = new Gtk.CellRendererText ();
			songColumn.PackStart (songTitleCell, true);

			Gtk.ListStore musicListStore = new Gtk.ListStore (typeof (Song));
			foreach (Song song in songs) {
				musicListStore.AppendValues (song);
			}

			artistColumn.SetCellDataFunc (artistNameCell, new Gtk.TreeCellDataFunc (RenderArtistName));
			songColumn.SetCellDataFunc (songTitleCell, new Gtk.TreeCellDataFunc (RenderSongTitle));

			tree.Model = musicListStore;

			tree.AppendColumn (artistColumn);
			tree.AppendColumn (songColumn);



		//	ScrolledWindow sw = new ScrolledWindow();

		//	sw.SetScrollAdjustments (adj1, adj1);
			//sw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);

			tree.SetScrollAdjustments (adj1, adj1);
			tree.HeightRequest = 100;
		//	sw.Add(tree);
		//	box1.Add (sw);
			box1.Add (tree);

			window.ShowAll ();
		}

		private static void RenderArtistName (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			Song song = (Song) model.GetValue (iter, 0);

			if (song.Artist.StartsWith ("X") == true) {
				(cell as Gtk.CellRendererText).Foreground = "red";
			} else {
				(cell as Gtk.CellRendererText).Foreground = "darkgreen";
			}

			(cell as Gtk.CellRendererText).Text = song.Artist;
		}

		private static void RenderSongTitle (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			Song song = (Song) model.GetValue (iter, 0);
			(cell as Gtk.CellRendererText).Text = song.Title;
		}

		public static void Main_ (string [] args)
		{
			Application.Init ();
			create_range_controls ();
			Application.Run ();
		}













		public static void Main (string[] args)
		{
			//TODO: will initializing MainController were have an effect on it's thread?

			Application.Init ();
			new MainWindow();
			Application.Run ();
		}

		public static void CloseApp() {
		//	EventBus.GetInstance().Close();
			WalletController.GetInstance().Quit();
			LogController.GetInstance().Quit();
			Application.Quit ();
		}
	}
}



//TODO: rename interfaces
//TODO: handle memory leaks for pixbufs
//TODO: redesign scrollbars
//TODO: use namespaces