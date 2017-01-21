<h1 align="center">EasyMySql.net</h1> 

Simple ERM framework for .net and Mysql

##Usage

<h>Step 1:</h2>

Set the MySql Connection string:

	 Settings.ConnectionString = "Server=YOURSERVER;Database=YourDB;Uid=USER;Pwd=PASSWORD";

<h>Step 2:</h2>
	 
Create a class that inherits the DataObject And decorate it with the <b>Ignore</b> and/or <b>Length</b> attributes

	public class MysqlObject : DataObject
    {
        public int IntValue { get; set; }
        public double DoubleValue { get; set; }
        public bool BoolValue { get; set; }
        [Length(100)]
        public string StringValue { get; set; }
        public DateTime DateTimeValue { get; set; }
        [Ignore]
        public bool IgnoredValue { get; set; }
        [Length(250)]
        public string LongerString { get; set; }
		[Unique]
		public int UniqueValue { get; set; }
    }
	
<h>Step 3:</h2>
	
Spawn a new <B>DataHandler</b> class

	var handler = new DataHandler<MysqlObject>()
	
or create a new handler class to get access to more functionality and/or Caching

	public class MysqlObjectHandler : DataHandler<MysqlObject>
    {
        public static readonly MysqlObjectHandler Instance = new MysqlObjectHandler();

        private MysqlObjectHandler()
        {

        }
    }
	
<h>Step 4:</h2>

Add or retrieve data with the <b>DataHandler</b>

	MysqlObjectHandler.instance.Add(new MysqlObject() { BoolValue = true, DateTimeValue = DateTime.Now, DoubleValue = 0.1, IgnoredValue = true, IntValue = 32, StringValue = "Teststring", UniqueValue = 2 });

	var Result = MysqlObjectHandler.instance.GetItems();

    var Result = new DataHandler<MysqlObject>().GetItems();
