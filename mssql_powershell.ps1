$sqlConn = New-Object System.Data.SqlClient.SqlConnection
$sqlConn.ConnectionString = "Server=localhost\SQLExpress;Integrated Security=true;Initial Catalog=master"
$sqlConn.Open()

$sqlcmd = $sqlConn.CreateCommand()
$query = "Create database CatalystDB"
$sqlcmd.CommandText = $query

$adp = New-Object System.Data.SqlClient.SqlDataAdapter $sqlcmd
$data = New-Object System.Data.DataSet
$adp.Fill($data) | Out-Null
$data.Tables

$query = "SELECT name, database_id FROM sys.databases"
$sqlcmd.CommandText = $query
$adp = New-Object System.Data.SqlClient.SqlDataAdapter $sqlcmd
$data = New-Object System.Data.DataSet
$adp.Fill($data) | Out-Null
$data.Tables

$sqlConn.Close()