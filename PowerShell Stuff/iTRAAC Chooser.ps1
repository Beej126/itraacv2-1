$cn = new-object system.data.SqlClient.SqlConnection("Data Source=$env:ITRAACCENTRALDB;User ID=$env:SQLCMDUSER;Password=$env:ITRAACCENTRALPASS");
$ds = new-object "System.Data.DataSet" "dsTaxOffices"
$q = "SELECT TaxOfficeName + ' ('+OfficeCode+')' as Name, OfficeCode FROM iTRAAC.dbo.tblTaxOffices (nolock) where Active = 1 order by TaxOfficeName"
$da = new-object "System.Data.SqlClient.SqlDataAdapter" ($q, $cn)
$da.Fill($ds)

# ping each box to see who's online
#foreach($row in $ds.Tables[0].Rows) {
#	$ipaddress = $row["IPAddress"]
#	$ping_result = Get-WmiObject -Class Win32_PingStatus -Filter "Address='$ipaddress'"
#	if ($ping_result.StatusCode -ne "0") {
#  	$row["Name"] = $row["Name"] + "`t* offline *"
#  }
#}

# Load Windows Forms Library
[System.Reflection.Assembly]::LoadWithPartialName("System.Windows.Forms") | Out-Null

#pointer for easy debug message box popups
$mb=[Windows.Forms.MessageBox]
#$mb::Show("you clicked me!  ", "Message")

# office listbox
$lst = new-object System.Windows.Forms.ListBox
$lst.Name = "listBox1"
$lst.Dock = "Fill"
$lst.AutoSize = "True"

# databind list of offices to listbox
$lst.ValueMember = "OfficeCode"
$lst.DisplayMember = "Name"
$lst.DataSource = $ds.Tables[0]

# select office button
$btnSelect = New-Object System.Windows.Forms.Button
$btnSelect.AutoSize = "True"
$btnSelect.Text = "          Select          ";

$btnSelect.add_click({
	#if you get an error here the quickest way i know how to resolve is to blast all the permissions on this key and give full access to the current user
	Set-ItemProperty hklm:\software\odbc\odbc.ini\itraac Server "mwr-tro-$($lst.SelectedValue)\mssql2008"
	Get-Process | Where-Object {$_.Name -eq "itraacw32"} | kill
	sleep 1 #wait a sec for the old process to really drop off, otherwise new one collides and stops itself
	start "c:\Program Files\iTRAAC Console\iTRAACw32.exe"
	$form.Close()
})

# cancel button
$btnCancel = New-Object System.Windows.Forms.Button
$btnCancel.AutoSize = "True"
$btnCancel.Text = "Close"
$btnCancel.add_click({
	$form.Close()
})

# Create Flow Panel for a "button bar", bottom docked
$panel = new-object "System.Windows.Forms.FlowLayoutPanel"
$panel.Dock = "Bottom"
$panel.AutoSize = "True"

# Add buttons to panel
$panel.Controls.Add($btnSelect)
$panel.Controls.Add($btnCancel)

# Create Window
$form = new-object "System.Windows.Forms.Form"
$form.AutoSize = "True"
$form.topmost = "True"
$form.text = "Choose Office"
$form.FormBorderStyle = "FixedToolWindow"

$form.add_load({
	#$mb::Show("onload", "onload")
	$form.WindowState = "Normal"
	(Get-Item hklm:\software\odbc\odbc.ini\itraac).GetValue("server") -match "mwr-tro-(\w+)"
	$lst.SelectedValue = $matches[1]
})

# Add controls to form
$form.Controls.Add($lst)
$form.Controls.Add($panel)
$form.AcceptButton = $btnSelect
$form.CancelButton = $btnCancel

# Show window
$form.showdialog()
$form.dispose()
