using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Web.Script.Serialization;

namespace MuEditorPhoenix {
 public static class Guard {
  public static string Hash(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","").ToLowerInvariant();}
  public static string Hex(byte[] b){return BitConverter.ToString(b).Replace("-","");}
  public static byte[] Unhex(string s){s=new string(s.Where(c=>!char.IsWhiteSpace(c)).ToArray());if(s.Length%2!=0)throw new Exception("Hexadecimal incompleto.");return Enumerable.Range(0,s.Length/2).Select(i=>Convert.ToByte(s.Substring(i*2,2),16)).ToArray();}
  public static void Atomic(string p,byte[] b){Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(p)));string tmp=p+"."+Guid.NewGuid().ToString("N")+".tmp";File.WriteAllBytes(tmp,b);if(File.Exists(p))File.Replace(tmp,p,null);else File.Move(tmp,p);}
  public static readonly string DefaultLocalData=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MUEditorPhoenix"); public static string LocalData=DefaultLocalData;
  public static void Backup(string label,object data){Directory.CreateDirectory(LocalData);string path=Path.Combine(LocalData,DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-ffff")+"-"+label+".protected");var raw=Encoding.UTF8.GetBytes(new JavaScriptSerializer{MaxJsonLength=int.MaxValue}.Serialize(data));File.WriteAllBytes(path,ProtectedData.Protect(raw,null,DataProtectionScope.CurrentUser));}
  public static XDocument Xml(string p){var settings=new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null};string text=File.ReadAllText(p);var declaration=System.Text.RegularExpressions.Regex.Match(text,@"<\?xml[^>]*encoding\s*=\s*['"" ]([^'"" ]+)['""]",System.Text.RegularExpressions.RegexOptions.IgnoreCase);if(declaration.Success&&!declaration.Groups[1].Value.StartsWith("utf",StringComparison.OrdinalIgnoreCase))text=File.ReadAllText(p,Encoding.GetEncoding(declaration.Groups[1].Value));var comments=new Dictionary<string,string>();text=System.Text.RegularExpressions.Regex.Replace(text,@"<!\[CDATA\[.*?\]\]>|<!--.*?-->",m=>{if(m.Value.StartsWith("<![CDATA["))return m.Value;string id="MUWORKBENCH-PRESERVED-COMMENT-"+Guid.NewGuid().ToString("N");comments[id]=m.Value;return "<!--"+id+"-->";},System.Text.RegularExpressions.RegexOptions.Singleline);using(var r=XmlReader.Create(new StringReader(text),settings)){var doc=XDocument.Load(r,LoadOptions.PreserveWhitespace);doc.AddAnnotation(comments);return doc;}}
  public static byte[] RestoreComments(byte[] data,XDocument source){var comments=source.Annotation<Dictionary<string,string>>();if(comments==null)return data;string text=Encoding.UTF8.GetString(data);foreach(var pair in comments)text=text.Replace("<!--"+pair.Key+"-->",pair.Value);return new UTF8Encoding(false).GetBytes(text);}
 }
 public class ItemDef {
  public int Section,Index,Width,Height;public string Name;public Dictionary<string,string> Attributes=new Dictionary<string,string>();
  public int Id{get{return Section*512+Index;}}
  public override string ToString(){return Section+":"+Index+" — "+Name;}
 }
 public partial class Catalog {
  public Dictionary<int,ItemDef> Items=new Dictionary<int,ItemDef>(); public string Source=""; public HashSet<int> SocketItems=new HashSet<int>();
  public void Load(string path){var next=new Dictionary<int,ItemDef>();var doc=Guard.Xml(path);
   foreach(var section in doc.Descendants().Where(e=>e.Name.LocalName=="Section")){
    int s;if(!int.TryParse((string)section.Attribute("Index"),out s))continue;
    foreach(var item in section.Elements().Where(e=>e.Name.LocalName=="Item")){
     int i,w,h;if(!int.TryParse((string)item.Attribute("Index"),out i))continue;
     if(s<0||s>255||i<0||i>=512)throw new Exception("Seção ou índice de Item.xml fora do formato section*512+index.");
     int.TryParse((string)item.Attribute("Width"),out w);int.TryParse((string)item.Attribute("Height"),out h);
     var d=new ItemDef{Section=s,Index=i,Width=Math.Max(1,w),Height=Math.Max(1,h),Name=(string)item.Attribute("Name")??("Item "+i)};
     d.Attributes=item.Attributes().ToDictionary(a=>a.Name.LocalName,a=>a.Value);if(next.ContainsKey(d.Id))throw new Exception("Item duplicado: "+d.Id);next.Add(d.Id,d);
    }
   }
   if(next.Count==0)throw new Exception("Nenhum Section/Item encontrado. Selecione Data/Item/Item.xml do seu emulador.");Items=next;Source=Path.GetFullPath(path);LoadSocketTypes(path);
  }
  public string Name(int id){ItemDef d;return Items.TryGetValue(id,out d)?d.Name:"Item não identificado #"+id;}
 }
 public class ShopFile {
  public XDocument Document;public string Source;public byte[] Original;public DataTable Table;public List<XElement> Elements;
  public void Load(string p){var doc=Guard.Xml(p);if(doc.Root.Name.LocalName!="Shop")throw new Exception("Este XML não é uma loja <Shop>.");Source=p;Original=File.ReadAllBytes(p);Document=doc;Elements=doc.Descendants().Where(e=>e.Name.LocalName=="Item").ToList();Table=new DataTable();Table.Columns.Add("Tab",typeof(string));
   foreach(var name in new[]{"Section","Type","Level","Durability","Skill","Luck","Option","Excellent","SetOption","Name"}.Concat(Elements.SelectMany(e=>e.Attributes()).Select(a=>a.Name.LocalName)).Distinct())Table.Columns.Add(name,typeof(string));
   foreach(var e in Elements){var r=Table.NewRow();r["Tab"]=(string)e.Parent.Attribute("Index")??"1";foreach(var a in e.Attributes())r[a.Name.LocalName]=a.Value;Table.Rows.Add(r);}
  }
  public byte[] Export(Catalog catalog){var clone=new XDocument(Document);var old=clone.Descendants().Where(e=>e.Name.LocalName=="Item").ToList();foreach(var e in old)e.Remove();
   foreach(DataRow row in Table.Rows){if(row.RowState==DataRowState.Deleted)continue;int section,index; if(!int.TryParse(row["Section"].ToString(),out section)||!int.TryParse(row["Type"].ToString(),out index)||!catalog.Items.ContainsKey(section*512+index))throw new Exception("Item da loja ausente do Item.xml: "+row["Section"]+":"+row["Type"]);
    foreach(var field in new[]{"Level","Skill","Luck","Option","Excellent"}){int n;if(!int.TryParse(row[field].ToString(),out n)||n<0||n>(field=="Level"?15:field=="Skill"||field=="Luck"?1:field=="Option"?7:63))throw new Exception("Valor inválido em "+field);}
    string tab=row["Tab"].ToString();int tabNo;if(!int.TryParse(tab,out tabNo)||tabNo<1)throw new Exception("Aba inválida.");var parent=clone.Root.Elements().FirstOrDefault(e=>e.Name.LocalName=="TabInfo"&&(string)e.Attribute("Index")==tab);if(parent==null){parent=new XElement("TabInfo",new XAttribute("Index",tab));clone.Root.Add(parent);}
    var item=new XElement("Item");foreach(DataColumn col in Table.Columns)if(col.ColumnName!="Tab"&&!string.IsNullOrEmpty(row[col].ToString()))item.SetAttributeValue(col.ColumnName,row[col]);parent.Add(item);
   }
   using(var ms=new MemoryStream()){using(var writer=XmlWriter.Create(ms,new XmlWriterSettings{Encoding=new UTF8Encoding(false),Indent=true}))clone.Save(writer);return Guard.RestoreComments(ms.ToArray(),Document);}
  }
  public void Add(ItemDef d){var r=Table.NewRow();foreach(DataColumn c in Table.Columns)r[c]="0";r["Tab"]="1";r["Section"]=d.Section;r["Type"]=d.Index;r["Name"]=d.Name;
   foreach(DataColumn c in Table.Columns)if(c.ColumnName.StartsWith("Socket")||c.ColumnName.StartsWith("HarmonyOptionEx"))r[c]="-1";Table.Rows.Add(r);
  }
 }
 public class TextFile {
  public string Source,Name;public byte[] Original;public Encoding Encoding; public bool At;public int Width; public DataTable Table;
  List<string> raw;List<int> indices;
  public static string[] PackageHeaders={"Category","DisplayOrder","PackageID","Name","PriceUnit","Price","Description","Unknown7","SalesFlag","GiftFlag","Unknown10","Unknown11","Unknown12","Unknown13","CurrencyName","CurrencyName2","Unknown16","Unknown17","Unknown18","ProductIDs","ItemCode","Unknown21","Unknown22","PriceIDs","Unknown24","Coin","Unknown26"};
  public static string[] ProductHeaders={"ProductID","Name","Property","Value","Unit","Price","PriceID","Unknown7","Unknown8","Unknown9","Unknown10","Unknown11","Unknown12","ItemCode","PropertyID","Unknown15","UnitType"};
  public void Load(string p){Source=p;Name=Path.GetFileName(p);Original=File.ReadAllBytes(p);Encoding=Detect(Original);string text=Encoding.GetString(Original);raw=text.Replace("\r\n","\n").TrimEnd('\r','\n').Split('\n').ToList();At=Name.StartsWith("IBS",StringComparison.OrdinalIgnoreCase);indices=new List<int>();var rows=new List<string[]>();
   for(int i=0;i<raw.Count;i++){string l=raw[i].Trim();if(l.Length==0||l.StartsWith("//")||l=="end")continue;var fields=At?raw[i].Split('@'):System.Text.RegularExpressions.Regex.Split(l.Split(new[]{"//"},StringSplitOptions.None)[0].Trim(),@"\s+");if(!At&&!char.IsDigit(fields[0][0]))continue;indices.Add(i);rows.Add(fields);}
   if(rows.Count==0)throw new Exception("Arquivo sem registros: "+Name);Width=rows[0].Length;if(rows.Any(r=>r.Length!=Width))throw new Exception("Colunas inconsistentes em "+Name+"; formato não reconhecido.");
   Table=new DataTable();string[] headers=null;if(Name.Equals("IBSPackage.txt",StringComparison.OrdinalIgnoreCase))headers=PackageHeaders;if(Name.Equals("IBSProduct.txt",StringComparison.OrdinalIgnoreCase))headers=ProductHeaders;
   if(Name.Equals("CashShopPackage.txt",StringComparison.OrdinalIgnoreCase))headers=new[]{"Category","PackageID","MainID","ItemCount","Coin","Price","Bonus","Gift"}.Concat(Enumerable.Range(1,10).Select(i=>"ProductBase"+i)).Concat(Enumerable.Range(1,10).Select(i=>"ProductMain"+i)).ToArray();
   if(Name.Equals("CashShopProduct.txt",StringComparison.OrdinalIgnoreCase))headers=new[]{"ProductBase","ProductMain","Price","ItemCode","Level","Skill","Luck","Option","Excellent","Ancient","Harmony","OptionEx","Socket1","Socket2","Socket3","Socket4","Socket5","Quantity","Duration"};
   for(int j=0;j<Width;j++)Table.Columns.Add(headers!=null&&j<headers.Length?headers[j]:"Field"+j,typeof(string));foreach(var row in rows)Table.Rows.Add(row.Cast<object>().ToArray());
  }
  static Encoding Detect(byte[] b){try{var utf=new UTF8Encoding(false,true);utf.GetString(b);return utf;}catch(DecoderFallbackException){return System.Text.Encoding.GetEncoding(1252);}}
  public IEnumerable<DataRow> Rows(){return Table.Rows.Cast<DataRow>().Where(r=>r.RowState!=DataRowState.Deleted);}
  public byte[] Export(){var output=new List<string>();int n=0;var rows=Rows().ToList();for(int i=0;i<raw.Count;i++){if(indices.Contains(i)){if(n<rows.Count){string value=Join(rows[n++]);if(!At&&raw[i].Contains("//"))value+="\t//"+raw[i].Split(new[]{"//"},2,StringSplitOptions.None)[1];output.Add(value);}}else if(raw[i].Trim()!="end")output.Add(raw[i]);}
   while(n<rows.Count)output.Add(Join(rows[n++]));if(!At&&raw.Any(l=>l.Trim()=="end"))output.Add("end");return Encoding.GetBytes(string.Join("\r\n",output)+"\r\n");}
  string Join(DataRow r){var fields=r.ItemArray.Select(v=>Convert.ToString(v)).ToArray();if(fields.Any(v=>v.Contains("\r")||v.Contains("\n")||(At&&v.Contains("@"))||(!At&&v.Any(char.IsWhiteSpace))))throw new Exception("Campo contém separador inválido em "+Name);return string.Join(At?"@":"\t",fields);}
 }
 public class CashCatalog {
  public Dictionary<string,TextFile> Files=new Dictionary<string,TextFile>(StringComparer.OrdinalIgnoreCase);
  public void Load(string p){var f=new TextFile();f.Load(p);Files[f.Name]=f;}
  public List<string> Validate(Catalog items){var errors=new List<string>();TextFile client,server,prod;
   if(Files.TryGetValue("IBSPackage.txt",out client)){
    if(client.Width!=27)errors.Add("Formato IBSPackage não suportado: esperado 27 campos.");else foreach(var r in client.Rows()){if(r[8].ToString()!="182")errors.Add("Indisponível no cliente: "+r[2]+" "+r[3]+" (SalesFlag "+r[8]+")");}
    if(Files.TryGetValue("CashShopPackage.txt",out server)&&server.Width==28){var ids=new HashSet<string>(server.Rows().Select(r=>r[2].ToString()));foreach(var r in client.Rows())if(!ids.Contains(r[2].ToString()))errors.Add("Sem pacote no servidor: "+r[2]+" — "+r[3]);}
   }
   if(Files.TryGetValue("CashShopPackage.txt",out server)&&Files.TryGetValue("CashShopProduct.txt",out prod)&&server.Width==28&&prod.Width==19){var pairs=new HashSet<string>(prod.Rows().Select(r=>r[0]+":"+r[1]));foreach(var r in server.Rows())for(int i=0;i<10;i++)if(r[8+i].ToString()!="0"&&!pairs.Contains(r[8+i]+":"+r[18+i]))errors.Add("Produto ausente no pacote "+r[2]+": "+r[8+i]+":"+r[18+i]);}
   if(Files.TryGetValue("CashShopProduct.txt",out prod)&&prod.Width==19){foreach(var r in prod.Rows()){int id;if(!int.TryParse(r[3].ToString(),out id)||!items.Items.ContainsKey(id))errors.Add("ItemCode não encontrado no Item.xml: "+r[3]);int q;if(!int.TryParse(r[17].ToString(),out q)||q<0)errors.Add("Quantidade inválida no produto "+r[1]);}}
   foreach(var f in Files.Values){int key=f.Name.Equals("IBSPackage.txt",StringComparison.OrdinalIgnoreCase)?2: f.Name.Equals("CashShopPackage.txt",StringComparison.OrdinalIgnoreCase)?2:-1;if(key>=0)foreach(var g in f.Rows().GroupBy(r=>r[key].ToString()).Where(g=>g.Count()>1))errors.Add("ID duplicado "+g.Key+" em "+f.Name);}
   return errors;
  }
  public void SetPrice(string id,decimal price){if(price<0||price>int.MaxValue||price!=decimal.Truncate(price))throw new Exception("Preço deve ser inteiro não negativo.");TextFile client,server,products,clientProducts;if(!Files.TryGetValue("IBSPackage.txt",out client)||!Files.TryGetValue("CashShopPackage.txt",out server)||!Files.TryGetValue("CashShopProduct.txt",out products)||!Files.TryGetValue("IBSProduct.txt",out clientProducts))throw new Exception("Importe os quatro arquivos de pacotes/produtos antes de sincronizar preços.");
   var c=client.Rows().SingleOrDefault(r=>r[2].ToString()==id);var s=server.Rows().SingleOrDefault(r=>r[2].ToString()==id);if(c==null||s==null)throw new Exception("Pacote precisa existir nos dois lados.");
   var ids=c[23].ToString().Split('|').Where(x=>x!="").ToArray();var productIds=c[19].ToString().Split('|').Where(x=>x!="").ToArray();if(ids.Length!=1||productIds.Length!=1||s[9].ToString()!="0")throw new Exception("Use as tabelas para pacotes com múltiplas opções ou bundles; preço não pode ser propagado automaticamente.");
   var sp=products.Rows().Where(r=>r[0].ToString()==productIds[0]&&r[1].ToString()==ids[0]).ToList();var cp=clientProducts.Rows().Where(r=>r[0].ToString()==productIds[0]&&r[6].ToString()==ids[0]).ToList();if(sp.Count!=1||cp.Count==0)throw new Exception("Vínculos de produto/preço inconsistentes.");
   if(client.Rows().Any(r=>r[2].ToString()!=id&&(r[19].ToString().Split('|').Contains(productIds[0])||r[23].ToString().Split('|').Contains(ids[0]))))throw new Exception("Produto ou opção de preço compartilhado por outro pacote. Revise manualmente para evitar alterar outra oferta.");
   string value=price.ToString(System.Globalization.CultureInfo.InvariantCulture);c[5]=value;s[5]=value;sp[0][2]=value;foreach(var r in cp)r[5]=value;
  }
 }
 public class InventoryBlob {
  public byte[] Original,Data;public const int SlotBytes=25;public const int Slots=239;
  public void Load(byte[] bytes){if(bytes==null||bytes.Length!=Slots*SlotBytes)throw new Exception("Perfil MuDevs 25×239 requer 5975 bytes. Outro layout deve usar um adaptador próprio.");Original=(byte[])bytes.Clone();Data=(byte[])bytes.Clone();}
  public byte[] Slot(int slot){if(slot<0||slot>=Slots)throw new Exception("Slot inválido.");return Data.Skip(slot*SlotBytes).Take(SlotBytes).ToArray();}
  public bool Empty(int slot){return Slot(slot).All(b=>b==255);}
  public int Id(int slot){var b=Slot(slot);return b[0]+((b[7]&128)<<1)+((b[9]&240)<<5)+((b[9]&1)<<13);}
  public void Set(int slot,byte[] bytes){if(bytes.Length!=SlotBytes)throw new Exception("Um slot deve ter 25 bytes.");Slot(slot);Buffer.BlockCopy(bytes,0,Data,slot*SlotBytes,SlotBytes);}
  public void Options(int slot,int level,int durability,bool skill,bool luck,int option,int excellent){if(Empty(slot))throw new Exception("Slot vazio.");if(level<0||level>15||durability<0||durability>255||option<0||option>7||excellent<0||excellent>63)throw new Exception("Opções fora do intervalo.");var b=Slot(slot);b[1]=(byte)((skill?128:0)|(level<<3)|(luck?4:0)|(option&3));b[2]=(byte)durability;b[7]=(byte)((b[7]&128)|((option&4)<<4)|excellent);Set(slot,b);}
  public void Move(int source,int target,Catalog catalog){if(Empty(source)||!Empty(target))throw new Exception("Origem deve ter item e destino precisa estar vazio.");ItemDef item;if(!catalog.Items.TryGetValue(Id(source),out item))throw new Exception("Item não identificado: movimentação bloqueada.");if(source<12||target<12||source>=76||target>=76)throw new Exception("Movimentação disponível somente na bolsa principal (slots 12..75). Equipamentos e expansões exigem regras próprias.");
   var occupied=new HashSet<int>();for(int s=12;s<76;s++){if(s==source||Empty(s))continue;ItemDef d;if(!catalog.Items.TryGetValue(Id(s),out d))throw new Exception("Há item desconhecido; não é seguro calcular a ocupação da grade.");foreach(int cell in Cells(s,d))if(!occupied.Add(cell))throw new Exception("Sobreposição já existente na grade.");}
   foreach(int cell in Cells(target,item))if(occupied.Contains(cell))throw new Exception("Item sobrepõe outro item.");Set(target,Slot(source));Set(source,Enumerable.Repeat((byte)255,SlotBytes).ToArray());
  }
  static IEnumerable<int> Cells(int slot,ItemDef d){int[] starts={12,76,108,140,172,204};int start=starts.Last(s=>s<=slot);int end=start==12?76:Math.Min(start+32,Slots);int x=(slot-start)%8,y=(slot-start)/8;if(x+d.Width>8||start+(y+d.Height-1)*8+x+d.Width>end)throw new Exception("Item atravessa limite da bolsa.");for(int dy=0;dy<d.Height;dy++)for(int dx=0;dx<d.Width;dx++)yield return slot+dy*8+dx;}
 }
 public partial class Database {
  public string ConnectionString;public string Account="",Character="";public DataTable OriginalAccount,OriginalCharacter;public InventoryBlob Inventory=new InventoryBlob();
  SqlConnection Open(){var c=new SqlConnection(ConnectionString);c.Open();return c;}
  public DataTable Query(string sql,params SqlParameter[] p){using(var c=Open())using(var cmd=new SqlCommand(sql,c)){cmd.CommandTimeout=15;cmd.Parameters.AddRange(p);var t=new DataTable();using(var a=new SqlDataAdapter(cmd))a.Fill(t);return t;}}
  public void Probe(){var required=new Dictionary<string,string[]>{{"MEMB_INFO",new[]{"memb___id","mail_addr","bloc_code","AccountLevel","AccountExpireDate"}},{"MEMB_STAT",new[]{"memb___id","ConnectStat"}},{"Character",new[]{"AccountID","Name","Class","Inventory","cLevel","Strength","Dexterity","Vitality","Energy","Leadership","LevelUpPoint","Money","ResetCount","MasterResetCount","CtlCode"}},{"CashShopData",new[]{"AccountID","WCoinC","WCoinP","GoblinPoint"}}};var t=Query("SELECT TABLE_NAME,COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA='dbo'");foreach(var pair in required)foreach(string col in pair.Value)if(!t.Rows.Cast<DataRow>().Any(r=>(string)r[0]==pair.Key&&(string)r[1]==col))throw new Exception("Adaptador MuDevs incompatível: falta dbo."+pair.Key+"."+col);}
  public DataTable Search(string term){return Query("SELECT TOP (100) m.memb___id AS Account,m.AccountLevel,m.AccountExpireDate,m.bloc_code,ISNULL(s.ConnectStat,0) AS Online FROM dbo.MEMB_INFO m LEFT JOIN dbo.MEMB_STAT s ON s.memb___id=m.memb___id WHERE m.memb___id LIKE @q ORDER BY m.memb___id",new SqlParameter("@q",SqlDbType.VarChar,110){Value="%"+term+"%"});}
  public DataTable LoadAccount(string account){Account=account;OriginalAccount=Query("SELECT m.memb___id,m.mail_addr,m.bloc_code,m.AccountLevel,m.AccountExpireDate,c.WCoinC,c.WCoinP,c.GoblinPoint FROM dbo.MEMB_INFO m LEFT JOIN dbo.CashShopData c ON c.AccountID=m.memb___id WHERE m.memb___id=@a",P("@a",account));if(OriginalAccount.Rows.Count!=1)throw new Exception("Conta não encontrada.");return OriginalAccount.Copy();}
  public DataTable Characters(){return Query("SELECT Name,cLevel,Class,ResetCount FROM dbo.Character WHERE AccountID=@a ORDER BY Name",P("@a",Account));}
  public DataTable LoadCharacter(string name){InventoryNewItems.Clear();Character=name;OriginalCharacter=Query("SELECT Name,cLevel,Strength,Dexterity,Vitality,Energy,Leadership,LevelUpPoint,Money,ResetCount,MasterResetCount,CtlCode,Inventory FROM dbo.Character WHERE Name=@n AND AccountID=@a",P("@n",name),P("@a",Account));if(OriginalCharacter.Rows.Count!=1)throw new Exception("Personagem não encontrado.");Inventory.Load((byte[])OriginalCharacter.Rows[0]["Inventory"]);var copy=OriginalCharacter.Copy();copy.Columns.Remove("Inventory");return copy;}
  static SqlParameter P(string n,string v){return new SqlParameter(n,SqlDbType.VarChar,10){Value=v};}
  void Offline(SqlConnection c,SqlTransaction tx){using(var cmd=new SqlCommand("SELECT ConnectStat FROM dbo.MEMB_STAT WITH(UPDLOCK,HOLDLOCK) WHERE memb___id=@a",c,tx)){cmd.Parameters.Add(P("@a",Account));object stat=cmd.ExecuteScalar();if(stat!=null&&stat!=DBNull.Value&&Convert.ToInt32(stat)!=0)throw new Exception("Conta conectada. Saia completamente do jogo antes de salvar.");}}
  static bool Equal(object a,object b){if(a is byte[]&&b is byte[])return ((byte[])a).SequenceEqual((byte[])b);return Equals(a,b);}
  void CurrentMatches(SqlConnection c,SqlTransaction tx,string sql,DataTable original,params SqlParameter[] ps){using(var cmd=new SqlCommand(sql,c,tx)){cmd.Parameters.AddRange(ps);using(var rd=cmd.ExecuteReader()){if(!rd.Read())throw new Exception("Registro removido.");foreach(DataColumn col in original.Columns)if(!Equal(rd[col.ColumnName],original.Rows[0][col]))throw new Exception("Os dados mudaram após a leitura. Recarregue antes de salvar.");}}}
  public void SaveAccount(DataTable edited){if(OriginalAccount==null||edited.Rows.Count!=1)throw new Exception("Carregue uma conta.");var r=edited.Rows[0];int level=Convert.ToInt32(r["AccountLevel"]);if(level<0||level>3)throw new Exception("AccountLevel deve ser 0..3.");if(r["bloc_code"].ToString()!="0"&&r["bloc_code"].ToString()!="1")throw new Exception("Bloqueio deve ser 0 ou 1.");if(r["mail_addr"].ToString().Length>50)throw new Exception("Email excede 50 caracteres.");foreach(var name in new[]{"WCoinC","WCoinP","GoblinPoint"})if(r[name]!=DBNull.Value&&Convert.ToInt32(r[name])<0)throw new Exception("Saldo negativo.");if(level>0&&(r["AccountExpireDate"]==DBNull.Value||Convert.ToDateTime(r["AccountExpireDate"])<=DateTime.Now))throw new Exception("VIP requer vencimento futuro.");
   using(var c=Open())using(var tx=c.BeginTransaction(IsolationLevel.Serializable)){Offline(c,tx);CurrentMatches(c,tx,"SELECT m.memb___id,m.mail_addr,m.bloc_code,m.AccountLevel,m.AccountExpireDate,c.WCoinC,c.WCoinP,c.GoblinPoint FROM dbo.MEMB_INFO m WITH(UPDLOCK,HOLDLOCK) LEFT JOIN dbo.CashShopData c WITH(UPDLOCK,HOLDLOCK) ON c.AccountID=m.memb___id WHERE m.memb___id=@a",OriginalAccount,P("@a",Account));Guard.Backup("account",new{account=Account,before=OriginalAccount.Rows[0].ItemArray,after=r.ItemArray});
    using(var cmd=new SqlCommand("UPDATE dbo.MEMB_INFO SET mail_addr=@email,bloc_code=@block,AccountLevel=@level,AccountExpireDate=@expiry WHERE memb___id=@a",c,tx)){cmd.Parameters.Add(P("@a",Account));cmd.Parameters.Add("@email",SqlDbType.VarChar,50).Value=r["mail_addr"];cmd.Parameters.Add("@block",SqlDbType.Char,1).Value=r["bloc_code"];cmd.Parameters.Add("@level",SqlDbType.Int).Value=level;cmd.Parameters.Add("@expiry",SqlDbType.SmallDateTime).Value=r["AccountExpireDate"];if(cmd.ExecuteNonQuery()!=1)throw new Exception("Atualização inesperada.");}
    if(new[]{"WCoinC","WCoinP","GoblinPoint"}.Any(n=>r[n]!=DBNull.Value)){using(var cmd=new SqlCommand("IF NOT EXISTS(SELECT 1 FROM dbo.CashShopData WITH(UPDLOCK,HOLDLOCK) WHERE AccountID=@a) INSERT dbo.CashShopData(AccountID,WCoinC,WCoinP,GoblinPoint) VALUES(@a,0,0,0); UPDATE dbo.CashShopData SET WCoinC=@c,WCoinP=@p,GoblinPoint=@g WHERE AccountID=@a",c,tx)){cmd.Parameters.Add(P("@a",Account));cmd.Parameters.Add("@c",SqlDbType.Int).Value=r["WCoinC"]==DBNull.Value?0:r["WCoinC"];cmd.Parameters.Add("@p",SqlDbType.Int).Value=r["WCoinP"]==DBNull.Value?0:r["WCoinP"];cmd.Parameters.Add("@g",SqlDbType.Int).Value=r["GoblinPoint"]==DBNull.Value?0:r["GoblinPoint"];cmd.ExecuteNonQuery();}}tx.Commit();}
   LoadAccount(Account);
  }
  public void SaveCharacter(DataTable edited){CommitCharacter(edited,false,0);}

 }
}
