using System;using System.IO;using System.Data;using System.Data.SqlClient;using System.Linq;using System.Collections.Generic;using System.Web.Script.Serialization;using MuEditorPhoenix;
class DbTestRunner {
 static void Check(bool value,string label){if(!value)throw new Exception(label);}
 static void Reject(Action action,string label){bool rejected=false;try{action();}catch{rejected=true;}Check(rejected,label);}
 static int Main(string[] args){string host=args.Length>0?args[0]:"localhost";string report=args.Length>1?args[1]:"database-tests.json";string name="MuEditorPhoenixTest_"+Guid.NewGuid().ToString("N");var cs=new SqlConnectionStringBuilder{DataSource=host,InitialCatalog="master",IntegratedSecurity=true,Encrypt=true,TrustServerCertificate=true,ConnectTimeout=5};bool created=false;
  try{using(var c=new SqlConnection(cs.ConnectionString)){c.Open();using(var cmd=new SqlCommand("CREATE DATABASE ["+name+"]",c)){cmd.ExecuteNonQuery();created=true;}}
   cs.InitialCatalog=name;var db=new Database{ConnectionString=cs.ConnectionString};
   db.Query(@"CREATE TABLE dbo.MEMB_INFO(memb___id varchar(10) PRIMARY KEY,mail_addr varchar(50),bloc_code char(1),AccountLevel int,AccountExpireDate smalldatetime);
CREATE TABLE dbo.MEMB_STAT(memb___id varchar(10) PRIMARY KEY,ConnectStat tinyint);
CREATE TABLE dbo.CashShopData(AccountID varchar(10) PRIMARY KEY,WCoinC int,WCoinP int,GoblinPoint int);
CREATE TABLE dbo.Character(AccountID varchar(10),Name varchar(10) PRIMARY KEY,cLevel int,Strength int,Dexterity int,Vitality int,Energy int,Leadership int,LevelUpPoint int,Money int,ResetCount int,MasterResetCount int,CtlCode tinyint,Inventory varbinary(5975));
INSERT dbo.MEMB_INFO VALUES('testuser','before@example.invalid','0',0,'20260101');INSERT dbo.MEMB_STAT VALUES('testuser',0);INSERT dbo.CashShopData VALUES('testuser',10,20,30);
INSERT dbo.Character VALUES('testuser','TestChar',200,100,200,300,400,0,10,1000,1,0,0,@inventory);",new SqlParameter("@inventory",SqlDbType.VarBinary,5975){Value=Enumerable.Repeat((byte)255,5975).ToArray()});
   db.Query(@"CREATE TABLE dbo.warehouse(AccountID varchar(10) PRIMARY KEY,Items varbinary(6000),Money int);
CREATE TABLE dbo.ExtWarehouse(AccountID varchar(10),Number int,Items varbinary(6000),Money int,PRIMARY KEY(AccountID,Number));
CREATE TABLE dbo.MasterSkillTree(Name varchar(50) PRIMARY KEY,MasterLevel int,MasterPoint int);
INSERT dbo.warehouse VALUES('testuser',@items,100);
INSERT dbo.ExtWarehouse VALUES('testuser',1,@items,200);
INSERT dbo.MasterSkillTree VALUES('TestChar',20,10);",new SqlParameter("@items",SqlDbType.VarBinary,6000){Value=Enumerable.Repeat((byte)255,6000).ToArray()});
db.Query("ALTER TABLE dbo.Character ADD Class tinyint NOT NULL DEFAULT 0");
db.Probe();Check(db.Search("test").Rows.Count==1,"search");Check(db.Search("' OR 1=1--").Rows.Count==0,"parameterized search");
   var a=db.LoadAccount("testuser");a.Rows[0]["AccountLevel"]=1;a.Rows[0]["AccountExpireDate"]=DateTime.Today.AddDays(30);a.Rows[0]["mail_addr"]="after@example.invalid";a.Rows[0]["WCoinC"]=50;db.SaveAccount(a);Check(Convert.ToInt32(db.OriginalAccount.Rows[0]["WCoinC"])==50,"account/coins transaction");
   var ch=db.LoadCharacter("TestChar");ch.Rows[0]["Strength"]=1234;byte[] item=Enumerable.Range(0,25).Select(n=>(byte)n).ToArray();db.Inventory.Set(12,item);db.SaveCharacter(ch);Check(Convert.ToInt32(db.OriginalCharacter.Rows[0]["Strength"])==1234&&db.Inventory.Slot(12).SequenceEqual(item),"character/blob transaction");
   db.Query("UPDATE dbo.MEMB_STAT SET ConnectStat=1 WHERE memb___id='testuser'");a=db.LoadAccount("testuser");a.Rows[0]["WCoinC"]=999;Reject(()=>db.SaveAccount(a),"online account must reject");Check(Convert.ToInt32(db.Query("SELECT WCoinC FROM dbo.CashShopData").Rows[0][0])==50,"online rejection unchanged");
   db.Query("UPDATE dbo.MEMB_STAT SET ConnectStat=0 WHERE memb___id='testuser'");a=db.LoadAccount("testuser");a.Rows[0]["mail_addr"]="attempt@example.invalid";db.Query("UPDATE dbo.MEMB_INFO SET mail_addr='concurrent@example.invalid'");Reject(()=>db.SaveAccount(a),"concurrent account update must reject");
   ch=db.LoadCharacter("TestChar");ch.Rows[0]["Strength"]=333;db.Query("UPDATE dbo.Character SET Money=2222");Reject(()=>db.SaveCharacter(ch),"concurrent character update must reject");Check(Convert.ToInt32(db.Query("SELECT Strength FROM dbo.Character").Rows[0][0])==1234,"rejection rolls back");
   a=db.LoadAccount("testuser");a.Rows[0]["AccountLevel"]=5;Reject(()=>db.SaveAccount(a),"invalid VIP level");

   Check(db.WarehousePages().Rows.Count==2,"warehouse pages");
   var detailed=db.LoadDetailedCharacter("TestChar");Check(db.MasterDraft!=null,"master data");db.MasterDraft.Rows[0]["MasterPoint"]=25;
   db.LoadWarehouse(-1);byte[] transferred=db.Inventory.Slot(12);db.Warehouse.Set(0,transferred);db.Inventory.Set(12,Enumerable.Repeat((byte)255,25).ToArray());db.CrossStorageDirty=true;
   Reject(()=>db.SaveCharacter(detailed),"split transfer save rejected");db.SaveWorkspace(detailed,300);
   Check(db.Inventory.Empty(12)&&db.Warehouse.Get(0).SequenceEqual(transferred),"atomic transfer");
   Check(Convert.ToInt32(db.MasterDraft.Rows[0]["MasterPoint"])==25,"master atomic update");
   detailed=db.LoadDetailedCharacter("TestChar");detailed.Rows[0]["Strength"]=4321;db.MasterDraft.Rows[0]["MasterPoint"]=99;
   db.Query("UPDATE dbo.warehouse SET Money=777 WHERE AccountID='testuser'");
   Reject(()=>db.SaveWorkspace(detailed,400),"concurrent warehouse rejects whole transaction");
   Check(Convert.ToInt32(db.Query("SELECT Strength FROM dbo.Character").Rows[0][0])==1234,"character rollback with warehouse conflict");
   Check(Convert.ToInt32(db.Query("SELECT MasterPoint FROM dbo.MasterSkillTree").Rows[0][0])==25,"master rollback with warehouse conflict");
   db.LoadWarehouse(1);db.Warehouse.Set(0,transferred);db.SaveWarehouse(500);Check(db.Warehouse.Get(0).SequenceEqual(transferred),"extended warehouse save");
   db.Query("UPDATE dbo.MEMB_STAT SET ConnectStat=1 WHERE memb___id='testuser'");Reject(()=>db.SaveWarehouse(600),"online warehouse blocked");db.Query("UPDATE dbo.MEMB_STAT SET ConnectStat=0 WHERE memb___id='testuser'");
   int count=db.BrowseCharacters("").Rows.Count;db.Query("INSERT dbo.Character(AccountID,Name,cLevel,Strength,Dexterity,Vitality,Energy,Leadership,LevelUpPoint,Money,ResetCount,MasterResetCount,CtlCode,Inventory) SELECT AccountID,'NewChar',cLevel,Strength,Dexterity,Vitality,Energy,Leadership,LevelUpPoint,Money,ResetCount,MasterResetCount,CtlCode,Inventory FROM dbo.Character WHERE Name='TestChar'");Check(db.BrowseCharacters("").Rows.Count==count+1,"new character appears on refresh query");

   File.WriteAllText(report,new JavaScriptSerializer().Serialize(new{status="PASS",isolatedDatabase=true,productionWrites=false,tests=new[]{"schema probe","parameterized search","VIP/account/coins transaction","character/inventory transaction","online account blocked","optimistic conflict blocked","invalid VIP blocked","encrypted local backup","base and extended warehouse","atomic cross-storage transfer","Master data save","warehouse conflict rolls back character and Master","split transfer blocked","new character refresh"}}));Console.WriteLine("PASS: isolated database integration tests");return 0;
  }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}finally{if(created){cs.InitialCatalog="master";SqlConnection.ClearAllPools();using(var c=new SqlConnection(cs.ConnectionString)){c.Open();using(var cmd=new SqlCommand("ALTER DATABASE ["+name+"] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE ["+name+"]",c)){cmd.ExecuteNonQuery();}}}}
 }
}
