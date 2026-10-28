using System;using System.IO;using System.Linq;using System.Drawing;using System.Drawing.Drawing2D;using System.Collections.Generic;using System.Windows.Forms;
namespace MuEditorPhoenix {
 public class ItemImages : IDisposable {
  public string Root="";Dictionary<int,Image> cache=new Dictionary<int,Image>();
  public Image Get(int id){Image result;if(cache.TryGetValue(id,out result))return result;int section=id/512,index=id%512;string path=Path.Combine(Root,section.ToString(),index+".png");if(string.IsNullOrEmpty(Root)||!File.Exists(path))return null;try{using(var original=Image.FromFile(path)){var b=new Bitmap(96,96);using(var g=Graphics.FromImage(b)){g.InterpolationMode=InterpolationMode.HighQualityBicubic;float scale=Math.Min(90f/original.Width,90f/original.Height);float w=original.Width*scale,h=original.Height*scale;g.DrawImage(original,(96-w)/2,(96-h)/2,w,h);}cache[id]=b;return b;}}catch{return null;}}
  public void Dispose(){foreach(var value in cache.Values)value.Dispose();cache.Clear();}
 }
 public class ItemDrag {public ItemBoard Source;public int Slot;}
 public class ItemBoard : Control {
  public ItemBuffer Buffer;public Catalog Catalog;public ItemImages Images;public int Start=12,End=76,Columns=8,Cell=40,Selected=-1;public bool Equipment=false;
  public Action<ItemBoard,int> SelectedItem;public Action<ItemBoard,int,ItemBoard,int> Dropped;ToolTip tooltip=new ToolTip();Font smallFont=new Font("Segoe UI",7);int down=-1;Point origin;
  public ItemBoard(){DoubleBuffered=true;AllowDrop=true;BackColor=Color.FromArgb(15,21,28);ForeColor=Color.White;ResizeBoard();}
  public void Configure(ItemBuffer data,int start,int end,bool equipment){Buffer=data;Start=start;End=end;Equipment=equipment;Columns=equipment?4:8;Selected=-1;ResizeBoard();Invalidate();}
  void ResizeBoard(){Width=Columns*Cell+2;Height=((Math.Max(1,End-Start)+Columns-1)/Columns)*Cell+2;}
  Rectangle Rect(int slot,int width,int height){return new Rectangle(1+(slot-Start)%Columns*Cell,1+(slot-Start)/Columns*Cell,width*Cell,height*Cell);}
  int Hit(Point point){int x=(point.X-1)/Cell,y=(point.Y-1)/Cell;int slot=Start+y*Columns+x;if(point.X<1||point.Y<1||x>=Columns||slot<Start||slot>=End)return -1;if(Buffer!=null)for(int i=Start;i<End;i++){if(Buffer.Empty(i))continue;ItemDef d;int w=1,h=1;if(!Equipment&&Catalog.Items.TryGetValue(Buffer.Id(i),out d)){w=d.Width;h=d.Height;}if(Rect(i,w,h).Contains(point))return i;}return slot;}
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using(var line=new Pen(Color.FromArgb(40,55,68)))for(int slot=Start;slot<End;slot++)e.Graphics.DrawRectangle(line,Rect(slot,1,1));if(Buffer==null||Catalog==null)return;
   for(int slot=Start;slot<End;slot++){if(Buffer.Empty(slot))continue;ItemDef def;int id=Buffer.Id(slot);bool known=Catalog.Items.TryGetValue(id,out def);int w=Equipment||!known?1:def.Width,h=Equipment||!known?1:def.Height;var rect=Rect(slot,w,h);rect.Inflate(-2,-2);using(var brush=new SolidBrush(Color.FromArgb(28,47,58)))e.Graphics.FillRectangle(brush,rect);using(var pen=new Pen(slot==Selected?Color.FromArgb(88,214,176):Color.FromArgb(157,123,58),slot==Selected?2:1))e.Graphics.DrawRectangle(pen,rect);
    Image img=Images==null?null:Images.Get(id);if(img!=null){float scale=Math.Min((rect.Width-6)/96f,(rect.Height-6)/96f);e.Graphics.DrawImage(img,rect.X+(rect.Width-96*scale)/2,rect.Y+(rect.Height-96*scale)/2,96*scale,96*scale);}else{string text=known?def.Name:"#"+id;TextRenderer.DrawText(e.Graphics,text,smallFont,rect,ForeColor,TextFormatFlags.WordBreak|TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);}
    int level=(Buffer.Get(slot)[1]>>3)&15;if(level>0)TextRenderer.DrawText(e.Graphics,"+"+level,Font,new Point(rect.X+2,rect.Y+1),Color.WhiteSmoke);
   }
  }
  protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);down=Hit(e.Location);origin=e.Location;if(down<0)return;Selected=down;Invalidate();if(SelectedItem!=null)SelectedItem(this,down);}
  protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);int slot=Hit(e.Location);if(slot>=0&&Buffer!=null&&!Buffer.Empty(slot)){string name=Catalog.Name(Buffer.Id(slot))+"  +"+((Buffer.Get(slot)[1]>>3)&15)+"\nSlot "+slot;if(tooltip.GetToolTip(this)!=name)tooltip.SetToolTip(this,name);}else tooltip.SetToolTip(this,"");if(e.Button==MouseButtons.Left&&down>=0&&Buffer!=null&&!Buffer.Empty(down)&&(Math.Abs(e.X-origin.X)>5||Math.Abs(e.Y-origin.Y)>5)){int s=down;down=-1;DoDragDrop(new ItemDrag{Source=this,Slot=s},DragDropEffects.Move);}}
  protected override void OnDragEnter(DragEventArgs e){base.OnDragEnter(e);e.Effect=e.Data.GetDataPresent(typeof(ItemDrag))?DragDropEffects.Move:DragDropEffects.None;}
  protected override void OnDragDrop(DragEventArgs e){base.OnDragDrop(e);var drag=e.Data.GetData(typeof(ItemDrag)) as ItemDrag;int slot=Hit(PointToClient(new Point(e.X,e.Y)));if(drag!=null&&slot>=0&&Dropped!=null)Dropped(drag.Source,drag.Slot,this,slot);}
  protected override void Dispose(bool disposing){if(disposing){tooltip.Dispose();smallFont.Dispose();}base.Dispose(disposing);}
 }
}
