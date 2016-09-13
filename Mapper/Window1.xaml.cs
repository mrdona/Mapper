/*
 * Created by SharpDevelop.
 * User: Dona
 * Date: 12/09/2016
 * Time: 13:50
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
//using System.Drawing;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.IO;

using System.Runtime.InteropServices;

using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using NBTSharp;

namespace Mapper
{
	/// <summary>
	/// Interaction logic for Window1.xaml
	/// </summary>
	public partial class Window1 : Window, INotifyPropertyChanged 
	{
		#region INotifyPropertyChanged
		
		public event PropertyChangedEventHandler PropertyChanged;
		private void OnPropertyChanged(string propertyName)
		{
			if (this.PropertyChanged != null)
			{
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
		
		#endregion
		
		#region MAP class
		
		private class MAP
		{
			public string Name {get;set;}
			public int XRow {get;set;}
			public int ZRow {get;set;}
			public TagCompound MBT {get;set;}
			public System.Drawing.Bitmap BITMAP {get;set;}
		}
		
		#endregion
		
		#region Properties
		
		private double currentMapSize = 128;
		/// <summary>
		/// côté d'un carré de map affiché 
		/// </summary>
		public double CurrentMapSize
	    {
	      get
	      { 
	          return currentMapSize;
	      }
	      set 
	      { 
	          currentMapSize= value;
	          this.OnPropertyChanged("CurrentMapSize");
	      }
	    }	
		
		/// <summary>
		/// taille des cartes prises en compte
		/// </summary>
		private int scale = 4;
		
		/// <summary>
		/// taille de la carte en bloc
		/// </summary>
		private int mapSize = 2048;
		
		/// <summary>
		/// niveau de zoom courant
		/// </summary>
	    private double currentZoom = 1;
	    
	    /// <summary>
	    /// Taille en pixel d'un fichier map_*.dat
	    /// </summary>
	    private int pixelSize = 128;
		
	    /// <summary>
	    /// Liste des cartes chargées
	    /// </summary>
		private List<MAP> Maps = new List<MAP>();

        /// <summary>
        /// Position actuelle du curseur
        /// </summary>
        private Point currentPos;

        #endregion

        #region COLORS DEFS
        private Color[] MapColors;
		private Color[] BaseMapColors = new Color[]
		{
			Color.FromRgb(0, 0, 0),
			Color.FromRgb(127, 178, 56),
			Color.FromRgb(247, 233, 163),
			Color.FromRgb(167, 167, 167),
			Color.FromRgb( 255, 0, 0),
			 Color.FromRgb(160, 160, 255),
			 Color.FromRgb(167, 167, 167),
			 Color.FromRgb(0, 124, 0),
			 Color.FromRgb(255, 255, 255),
			 Color.FromRgb(164, 168, 184),
			 Color.FromRgb(183, 106, 47),
			 Color.FromRgb(112, 112, 112),
			 Color.FromRgb(64, 64, 255),
			 Color.FromRgb(104, 83, 50),
			//new 1.7 colors (13w42a/13w42b)
			 Color.FromRgb(255, 252, 245),
			 Color.FromRgb(216, 127, 51),
			 Color.FromRgb(178, 76, 216),
			 Color.FromRgb(102, 153, 216),
			 Color.FromRgb(229, 229, 51),
			 Color.FromRgb(127, 204, 25),
			 Color.FromRgb(242, 127, 165),
			 Color.FromRgb(76, 76, 76),
			 Color.FromRgb(153, 153, 153),
			 Color.FromRgb(76, 127, 153),
			 Color.FromRgb(127, 63, 178),
			 Color.FromRgb(51, 76, 178),
			 Color.FromRgb(102, 76, 51),
			 Color.FromRgb(102, 127, 51),
			 Color.FromRgb(153, 51, 51),
			 Color.FromRgb(25, 25, 25),
			 Color.FromRgb(250, 238, 77),
			 Color.FromRgb(92, 219, 213),
			 Color.FromRgb(74, 128, 255),
			 Color.FromRgb(0, 217, 58),
			 Color.FromRgb(21, 20, 31),
			 Color.FromRgb(112, 2, 0),
			//new 1.8 colors
			 Color.FromRgb(126, 84, 48)
		};
		#endregion
		
        /// <summary>
        /// Constructeur, point d'entrée
        /// </summary>
		public Window1()
		{
			InitializeComponent();
			
			GenerateMapColors();
			
			Load();

            
		}
		
        /// <summary>
        /// Charge et affiche les cartes
        /// </summary>
		private void Load()
		{
			try
			{			
                //reset des zoom / position
				currentZoom = 1;
				currentMapSize = 128;
				lblPos.Content = null;
				lblZoom.Content = null;
				grd_Main.Children.Clear();
				grd_Main.ColumnDefinitions.Clear();
				grd_Main.RowDefinitions.Clear();				
				this.UpdateLayout();

                //Lignes et colonnes max et min
                int xmin = 0, xmax = 0, zmin = 0, zmax = 0;

                //boucle sur les fichiers map
                foreach (string element in Directory.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory + @"maps")) 
				{
					string filename = System.IO.Path.GetFileName(element);
					
					if(filename.StartsWith(@"map_") && filename.EndsWith(@".dat"))
					{
						NBTFileReader reader = new NBTFileReader(element);
						TagCompound root = reader.compound;
						TagByteArray tagColors = (TagByteArray)root.getNode(@"\data\colors");
						var mapScale = (TagByte)root.getNode(@"\data\scale");
						if(mapScale.number == scale)
						{		
                            //recuperation du centre										
							var xCenter = ((TagInt)root.getNode(@"\data\xCenter")).number;
							var zCenter = ((TagInt)root.getNode(@"\data\zCenter")).number;
							
                            //calcul de la ligne et de la colonne
							var xrow = (int)Math.Floor((decimal)xCenter/mapSize);						
							if(xrow > xmax)
								xmax = xrow;
							if(xrow < xmin)
								xmin = xrow;							
							var zrow = (int)Math.Floor((decimal)zCenter/mapSize);
							if(zrow > zmax)
								zmax = zrow;
							if(zrow < zmin)
								zmin = zrow;
							
                            //creation de l'image de la carte
							var bitmap = GetMapBitmap(tagColors);
							
                            //ajout a la liste des cartes
							Maps.Add(new MAP(){Name = filename, XRow = xrow, ZRow = zrow, MBT = root, BITMAP = bitmap});
													
						}
					}
				}

                //Construction de la Grid d'affichage
				var totalXrows = (int)Math.Abs(xmin) + (int)Math.Abs(xmax);
				var totalZrows = (int)Math.Abs(zmin) + (int)Math.Abs(zmax);
				
                //Colonnes - axe X
				for(int i = 0; i<=totalXrows; i++)
				{
					var colDefinition = new ColumnDefinition();
					colDefinition.Width = GridLength.Auto;
					grd_Main.ColumnDefinitions.Add(colDefinition);
					
                    //binding pour la largeur des carrés en fonction du zoom
					Binding myBinding = new Binding();
					myBinding.Source = this;
					myBinding.Path = new PropertyPath("CurrentMapSize");
					myBinding.Mode = BindingMode.TwoWay;
					myBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
					BindingOperations.SetBinding(colDefinition, ColumnDefinition.MinWidthProperty, myBinding);
				}	

                //Lignes - axes Z
				for(int i = 0; i<=totalZrows; i++)
				{
					var rowDefinition = new RowDefinition();
					rowDefinition.Height = GridLength.Auto;
					grd_Main.RowDefinitions.Add(rowDefinition);

                    //binding pour la largeur des carrés en fonction du zoom
                    Binding myBinding = new Binding();
					myBinding.Source = this;
					myBinding.Path = new PropertyPath("CurrentMapSize");
					myBinding.Mode = BindingMode.TwoWay;
					myBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
					BindingOperations.SetBinding(rowDefinition, RowDefinition.MinHeightProperty, myBinding);
				}				

                //creation des controls Images pour les bitmap générées
				foreach (var map in Maps) 
				{
					this.Dispatcher.BeginInvoke(new Action(() => 
                       {
                           //creation imagesource from bitmap
                           var imgsrc = ImageSourceForBitmap(map.BITMAP);
                           var image = new Image(){Source = imgsrc};

                           //affectation ligne colonne
                           int row = map.ZRow - zmin;
                           int col = map.XRow - xmin;
                           Grid.SetRow(image, row);
                           Grid.SetColumn(image, col);

                           //Event pour la position
                           image.MouseMove +=HandleMouseEventHandler;
                           //event pour la suppression
                           image.MouseRightButtonDown +=HandleMouseButtonRightEventHandler;
                           //Event au clic gauche
                           image.MouseLeftButtonDown +=HandleMouseButtonLeftEventHandler;

                           image.ToolTip = map.Name;
                           grd_Main.Children.Add(image);
                           this.UpdateLayout();
					}));					
				}
			}
			catch(Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}
			
		/// <summary>
		/// OBSOLETE : mode d'affichage en Grid, trop de lenteurs
		/// </summary>
		/// <param name="mapArray"></param>
		/// <returns></returns>
		private Grid GetMapGrid(TagByteArray mapArray)
		{
			try
			{
				int zoom = 1;
				Grid grd = new Grid();
						
				for(int i = 0; i<128; i++)
				{
					var rowDefinition = new RowDefinition();
					rowDefinition.Height = new GridLength(zoom);
					grd.RowDefinitions.Add(rowDefinition);
				}
				for(int i = 0; i<128; i++)
				{
					var colDefinition = new ColumnDefinition();
					colDefinition.Width = new GridLength(zoom);
					grd.ColumnDefinitions.Add(colDefinition);
				}
				
				for(int i = 0; i<mapArray.array.Length; i++)
				{
					var first = mapArray.array[i];
					var color = MapColors[first];
					
					Border rec = new Border();
					var brush = new SolidColorBrush(color);
					rec.Background = brush;
					
					var row = (int)Math.Floor((decimal)i/128);
					
					Grid.SetRow(rec, row);
					Grid.SetColumn(rec, i - (row * 128) );
					grd.Children.Add(rec);
					
				}				
				
				return grd;
			}
			catch(Exception ex)
			{
				MessageBox.Show(ex.Message);
			}			
			
			return null;
		}
		
		/// <summary>
		/// Generation de la Bitmap de la map
		/// </summary>
		/// <param name="mapArray"></param>
		/// <returns></returns>
		private System.Drawing.Bitmap GetMapBitmap(TagByteArray mapArray)
		{
			try
			{
		        var image1 = new System.Drawing.Bitmap(128,128);		        
				for(int i = 0; i<mapArray.array.Length; i++)
				{
					var first = mapArray.array[i];
					var color = MapColors[first];
					
					var row = (int)Math.Floor((decimal)i/128);				
					image1.SetPixel(i - (row * 128), row, System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B));
				}
		        
				return image1;
			}
			catch(Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
			return null;
		}

		/// <summary>
		/// Generation des tables de couleurs
		/// </summary>
		private void GenerateMapColors()
		{
			try
			{
				MapColors = new Color[BaseMapColors.Length*4];
				for(int i = 0; i < BaseMapColors.Length; ++i)
				{
					Color bc = BaseMapColors[i];
					
					MapColors[i*4 +0] = Color.FromArgb(bc.A, (byte) (bc.R*180.0/255.0+0.5)
							                              , (byte) (bc.G*180.0/255.0+0.5)
							                              , (byte) (bc.B*180.0/255.0+0.5) );
					MapColors[i*4 +1] = Color.FromArgb(bc.A, (byte) (bc.R*220.0/255.0+0.5)
							                              , (byte) (bc.G*220.0/255.0+0.5)
							                              , (byte) (bc.B*220.0/255.0+0.5) );
					MapColors[i*4 +2] = bc;
					MapColors[i*4 +3] = Color.FromArgb(bc.A, (byte) (bc.R*135.0/255.0+0.5)
							                              , (byte) (bc.G*135.0/255.0+0.5)
							                              , (byte) (bc.B*135.0/255.0+0.5) );
				};				
			}
			catch(Exception ex)
			{
				MessageBox.Show(ex.Message);
			}			
		}

	    /// <summary>
	    /// Zoom
	    /// </summary>
	    /// <param name="sender"></param>
	    /// <param name="e"></param>
		void grd_Main_MouseWheel(object sender, MouseWheelEventArgs e)
		{
			try 
			{
				var imgscale =  e.Delta > 0 ? currentZoom + 0.1 : currentZoom - 0.1;
				currentZoom = imgscale;
				foreach (var child in grd_Main.Children)
				{
					var img = child as Image;
					if(img != null)
						img.LayoutTransform = new ScaleTransform() {ScaleX = imgscale, ScaleY = imgscale};
				} 
				
				lblZoom.Content = String.Format("Zoom: {0}", currentZoom);
				
				this.UpdateLayout();
				
				var dummy = ((Image)grd_Main.Children[0]);
				var	size = dummy.DesiredSize.Width;
				CurrentMapSize = size;
				
				e.Handled=true;
			} 
			catch (Exception ex)
			{
				
				MessageBox.Show(ex.Message);
			}
		}
				
		/// <summary>
		/// Position
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void HandleMouseEventHandler(object sender, MouseEventArgs e)
		{
            var img = sender as Image;
            if (img == null) return;
            try
            {
                var pos = e.GetPosition(img);
                var map = Maps.Find(m => m.Name.Equals(img.ToolTip));

                var xmap = (pos.X - (pixelSize / 2));
                var zmap = (pos.Y - (pixelSize / 2));

                var ratio = mapSize / pixelSize;

                var mXCenter = ((TagInt)map.MBT.getNode(@"\data\xCenter")).number;
                var mZCenter = ((TagInt)map.MBT.getNode(@"\data\zCenter")).number;

                var xCenter = Math.Floor((double)mXCenter / ratio);
                var zCenter = Math.Floor((double)mZCenter / ratio);

                var approxX = Math.Floor((xmap + xCenter) * ratio);
                var approxZ = Math.Floor((zmap + zCenter) * ratio);

                currentPos = new Point(approxX, approxZ);

                lblPos.Content = String.Format("Curseur X:{0} - Z:{1}", pos.X, pos.Y);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
		
		/// <summary>
		/// Suppression carte
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void HandleMouseButtonRightEventHandler(object sender, MouseButtonEventArgs e)
		{
			try
			{			
				var img = sender as Image;
				if (img == null) return;
				grd_Main.Children.Remove(img);
			}
			catch(Exception ex)
			{
				MessageBox.Show(ex.Message);
			}	
		}

        /// <summary>
        /// Ajout d'un POI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		void HandleMouseButtonLeftEventHandler(object sender, MouseButtonEventArgs e)
		{
			try
			{
				var wpPos = currentPos;
								
			}
			catch(Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		/// <summary>
		/// Bouton Rafraîchir
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
        void BtnRefresh_Click(object sender, RoutedEventArgs e)
		{
        	Load();
		}

		#region deplacement souris
		
	    Point scrollMousePoint = new Point();
	    double hOff = 1;
	    double wOff = 1;
	    private void scrollViewer_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	    {
	        scrollViewer.CaptureMouse();
	        scrollMousePoint = e.GetPosition(scrollViewer);
	        hOff = scrollViewer.HorizontalOffset;
	        wOff = scrollViewer.VerticalOffset;
	    }
	
	    private void scrollViewer_PreviewMouseMove(object sender, MouseEventArgs e)
	    {
	        if(scrollViewer.IsMouseCaptured)
	        {
	            scrollViewer.ScrollToHorizontalOffset(hOff + (scrollMousePoint.X - e.GetPosition(scrollViewer).X));
	            scrollViewer.ScrollToVerticalOffset(wOff + (scrollMousePoint.Y - e.GetPosition(scrollViewer).Y));
	        }
	    }
	    
        private void scrollViewer_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	    {
	        scrollViewer.ReleaseMouseCapture();
	    }
		
        private void scrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
		{
            //Pour centrer sur la souris vu que le scrollviewer scroll en auto sur le mousewheel
			e.Handled = true;
            scrollViewer.ScrollToHorizontalOffset(hOff + (scrollMousePoint.X - e.GetPosition(scrollViewer).X));
            scrollViewer.ScrollToVerticalOffset(wOff + (scrollMousePoint.Y - e.GetPosition(scrollViewer).Y));

            //pour effctuer le dezoom
			grd_Main_MouseWheel(sender, e);
		}

        #endregion

        #region generation BitmapSource

        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);

        public ImageSource ImageSourceForBitmap(System.Drawing.Bitmap bmp)
        {
            var handle = bmp.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally { DeleteObject(handle); }
        }

        #endregion

    }
}