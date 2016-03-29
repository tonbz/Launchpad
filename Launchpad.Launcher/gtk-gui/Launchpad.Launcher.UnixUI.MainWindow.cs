
// This file has been generated by the GUI designer. Do not modify.
namespace Launchpad.Launcher.UnixUI
{
	public partial class MainWindow
	{
		private global::Gtk.UIManager UIManager;
		
		private global::Gtk.Action ActionsAction1;
		
		private global::Gtk.Action refreshAction1;
		
		private global::Gtk.VBox vbox1;
		
		private global::Gtk.MenuBar menubar1;
		
		private global::Gtk.HBox hbox2;
		
		private global::Gtk.VBox BrowserContainer;
		
		private global::Gtk.Alignment alignment2;
		
		private global::Gtk.ScrolledWindow ScrolledBrowserWindow;
		
		private global::Gtk.Alignment alignment1;
		
		private global::Gtk.Label IndicatorLabel;
		
		private global::Gtk.HBox hbox3;
		
		private global::Gtk.Alignment alignment4;
		
		private global::Gtk.ProgressBar MainProgressBar;
		
		private global::Gtk.HBox hbox4;
		
		private global::Gtk.Alignment alignment3;
		
		private global::Gtk.Button PrimaryButton;

		protected virtual void Build ()
		{
			global::Stetic.Gui.Initialize (this);
			// Widget Launchpad.Launcher.UnixUI.MainWindow
			this.UIManager = new global::Gtk.UIManager ();
			global::Gtk.ActionGroup w1 = new global::Gtk.ActionGroup ("Default");
			this.ActionsAction1 = new global::Gtk.Action ("ActionsAction1", global::Mono.Unix.Catalog.GetString ("Actions"), null, null);
			this.ActionsAction1.ShortLabel = global::Mono.Unix.Catalog.GetString ("Actions");
			w1.Add (this.ActionsAction1, null);
			this.refreshAction1 = new global::Gtk.Action ("refreshAction1", global::Mono.Unix.Catalog.GetString ("Repair Game"), null, "gtk-refresh");
			this.refreshAction1.ShortLabel = global::Mono.Unix.Catalog.GetString ("Repair Game");
			w1.Add (this.refreshAction1, null);
			this.UIManager.InsertActionGroup (w1, 0);
			this.AddAccelGroup (this.UIManager.AccelGroup);
			this.Name = "Launchpad.Launcher.UnixUI.MainWindow";
			this.Title = global::Mono.Unix.Catalog.GetString ("Launchpad - <GameName>");
			this.Icon = global::Gdk.Pixbuf.LoadFromResource ("Launchpad.Launcher.Resources.RocketIcon.ico");
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			this.DefaultWidth = 755;
			this.DefaultHeight = 412;
			// Container child Launchpad.Launcher.UnixUI.MainWindow.Gtk.Container+ContainerChild
			this.vbox1 = new global::Gtk.VBox ();
			this.vbox1.Name = "vbox1";
			this.vbox1.Spacing = 6;
			// Container child vbox1.Gtk.Box+BoxChild
			this.UIManager.AddUiFromString ("<ui><menubar name='menubar1'><menu name='ActionsAction1' action='ActionsAction1'><menuitem name='refreshAction1' action='refreshAction1'/></menu></menubar></ui>");
			this.menubar1 = ((global::Gtk.MenuBar)(this.UIManager.GetWidget ("/menubar1")));
			this.menubar1.Name = "menubar1";
			this.vbox1.Add (this.menubar1);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.vbox1 [this.menubar1]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.hbox2 = new global::Gtk.HBox ();
			this.hbox2.Name = "hbox2";
			this.hbox2.Spacing = 6;
			this.hbox2.BorderWidth = ((uint)(4));
			// Container child hbox2.Gtk.Box+BoxChild
			this.BrowserContainer = new global::Gtk.VBox ();
			this.BrowserContainer.Name = "BrowserContainer";
			this.BrowserContainer.Spacing = 6;
			// Container child BrowserContainer.Gtk.Box+BoxChild
			this.alignment2 = new global::Gtk.Alignment (0.5F, 0.5F, 1F, 1F);
			this.alignment2.WidthRequest = 300;
			this.alignment2.Name = "alignment2";
			// Container child alignment2.Gtk.Container+ContainerChild
			this.ScrolledBrowserWindow = new global::Gtk.ScrolledWindow ();
			this.ScrolledBrowserWindow.WidthRequest = 290;
			this.ScrolledBrowserWindow.HeightRequest = 300;
			this.ScrolledBrowserWindow.CanFocus = true;
			this.ScrolledBrowserWindow.Name = "ScrolledBrowserWindow";
			this.ScrolledBrowserWindow.ShadowType = ((global::Gtk.ShadowType)(1));
			this.alignment2.Add (this.ScrolledBrowserWindow);
			this.BrowserContainer.Add (this.alignment2);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.BrowserContainer [this.alignment2]));
			w4.Position = 0;
			this.hbox2.Add (this.BrowserContainer);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.hbox2 [this.BrowserContainer]));
			w5.Position = 0;
			w5.Expand = false;
			this.vbox1.Add (this.hbox2);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.vbox1 [this.hbox2]));
			w6.Position = 1;
			// Container child vbox1.Gtk.Box+BoxChild
			this.alignment1 = new global::Gtk.Alignment (0.5F, 0.5F, 1F, 1F);
			this.alignment1.Name = "alignment1";
			this.alignment1.LeftPadding = ((uint)(6));
			// Container child alignment1.Gtk.Container+ContainerChild
			this.IndicatorLabel = new global::Gtk.Label ();
			this.IndicatorLabel.Name = "IndicatorLabel";
			this.IndicatorLabel.Xalign = 0F;
			this.IndicatorLabel.LabelProp = global::Mono.Unix.Catalog.GetString ("Idle");
			this.alignment1.Add (this.IndicatorLabel);
			this.vbox1.Add (this.alignment1);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.vbox1 [this.alignment1]));
			w8.Position = 2;
			w8.Expand = false;
			w8.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.hbox3 = new global::Gtk.HBox ();
			this.hbox3.Name = "hbox3";
			this.hbox3.Spacing = 6;
			this.hbox3.BorderWidth = ((uint)(4));
			// Container child hbox3.Gtk.Box+BoxChild
			this.alignment4 = new global::Gtk.Alignment (0.5F, 0.5F, 1F, 1F);
			this.alignment4.Name = "alignment4";
			// Container child alignment4.Gtk.Container+ContainerChild
			this.MainProgressBar = new global::Gtk.ProgressBar ();
			this.MainProgressBar.Name = "MainProgressBar";
			this.alignment4.Add (this.MainProgressBar);
			this.hbox3.Add (this.alignment4);
			global::Gtk.Box.BoxChild w10 = ((global::Gtk.Box.BoxChild)(this.hbox3 [this.alignment4]));
			w10.Position = 0;
			// Container child hbox3.Gtk.Box+BoxChild
			this.hbox4 = new global::Gtk.HBox ();
			this.hbox4.Name = "hbox4";
			this.hbox4.Spacing = 6;
			// Container child hbox4.Gtk.Box+BoxChild
			this.alignment3 = new global::Gtk.Alignment (0.5F, 0.5F, 1F, 1F);
			this.alignment3.WidthRequest = 100;
			this.alignment3.Name = "alignment3";
			// Container child alignment3.Gtk.Container+ContainerChild
			this.PrimaryButton = new global::Gtk.Button ();
			this.PrimaryButton.Sensitive = false;
			this.PrimaryButton.CanDefault = true;
			this.PrimaryButton.CanFocus = true;
			this.PrimaryButton.Name = "PrimaryButton";
			this.PrimaryButton.UseUnderline = true;
			this.PrimaryButton.Label = global::Mono.Unix.Catalog.GetString ("Inactive");
			this.alignment3.Add (this.PrimaryButton);
			this.hbox4.Add (this.alignment3);
			global::Gtk.Box.BoxChild w12 = ((global::Gtk.Box.BoxChild)(this.hbox4 [this.alignment3]));
			w12.Position = 0;
			this.hbox3.Add (this.hbox4);
			global::Gtk.Box.BoxChild w13 = ((global::Gtk.Box.BoxChild)(this.hbox3 [this.hbox4]));
			w13.Position = 1;
			w13.Expand = false;
			w13.Fill = false;
			this.vbox1.Add (this.hbox3);
			global::Gtk.Box.BoxChild w14 = ((global::Gtk.Box.BoxChild)(this.vbox1 [this.hbox3]));
			w14.Position = 3;
			w14.Expand = false;
			w14.Fill = false;
			this.Add (this.vbox1);
			if ((this.Child != null)) {
				this.Child.ShowAll ();
			}
			this.PrimaryButton.HasDefault = true;
			this.Show ();
			this.DeleteEvent += new global::Gtk.DeleteEventHandler (this.OnDeleteEvent);
			this.refreshAction1.Activated += new global::System.EventHandler (this.OnRepairGameActionActivated);
			this.PrimaryButton.Clicked += new global::System.EventHandler (this.OnPrimaryButtonClicked);
		}
	}
}