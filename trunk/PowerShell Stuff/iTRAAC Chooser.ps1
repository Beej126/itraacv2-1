$cn = new-object system.data.SqlClient.SqlConnection("Data Source=blah;User ID=blah;Password=blah");
$ds = new-object "System.Data.DataSet" "dsTaxOffices"
$q = "SELECT TaxOfficeName + ' ('+OfficeCode+')' as Name, IPAddress FROM iTRAAC.dbo.tblTaxOffices (nolock) where Active = 1 order by TaxOfficeName"
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
[System.Reflection.Assembly]::LoadWithPartialName("System.windows.forms") | Out-Null

#pointer for easy debug message box popups
$mb=[Windows.Forms.MessageBox]
#$mb::Show("you clicked me!  ", "Message")

# Create Flow Panel
#$panel = new-object "System.Windows.Forms.flowlayoutpanel"
#$panel.Dock = [System.Windows.Forms.DockStyle]::Fill
#$form.Controls.Add($panel)

# Create Controls
$lst = new-object System.Windows.Forms.ListBox
#$lst.FormattingEnabled = true;
#$lst.TabIndex = 0;
$lst.Location = New-Object System.Drawing.Point(3, 3);
$lst.Name = "listBox1";
$lst.Size = New-Object System.Drawing.Size @(285, 320);

# databind list of offices to listbox
$lst.ValueMember = "IPAddress"
$lst.DisplayMember = "Name"
$lst.DataSource = $ds.Tables[0]

$btnSelect = New-Object System.Windows.Forms.Button
$btnSelect.Location = New-Object System.Drawing.Point @(65, 325)
#$btnSelect.Name = "button1";
$btnSelect.Size = New-Object System.Drawing.Size @(159, 23);
#$btnSelect.TabIndex = 1;
$btnSelect.Text = "Select";
#$btnSelect.UseVisualStyleBackColor = true;

$btnSelect.add_click({
	#if you get an error here the quickest way i know how to resolve is to blast all the permissions on this key and give full access to the current user
	Set-ItemProperty hklm:\software\odbc\odbc.ini\itraac Server $lst.SelectedItem.IPAddress
	Get-Process | Where-Object {$_.Name -eq "itraacw32"} | kill
	sleep 1 #wait a sec for the old process to really drop off, otherwise new one collides and stops itself
	start "c:\Program Files\iTRAAC Console\iTRAACw32.exe"
	$form.Close()
})

$btnCancel = New-Object System.Windows.Forms.Button
$btnCancel.add_click({
	$form.Close()
})

# Create Window
$form = new-object "System.Windows.Forms.Form"
$form.Size = new-object System.Drawing.Size @(300,377)
$form.topmost = $true
$form.text = "Choose Office"
$form.FormBorderStyle = "FixedToolWindow"

$form.add_load({
	#$mb::Show("onload", "onload")
	$form.WindowState = "Normal"
	$lst.SelectedValue = (Get-Item hklm:\software\odbc\odbc.ini\itraac).GetValue("server")
})

$form.AcceptButton = $btnSelect
$form.CancelButton = $btnCancel

# Add controls to Panel
$form.Controls.Add($lst)
$form.Controls.Add($btnSelect)
$form.Controls.Add($btnCancel)

# Show window
$form.showdialog()
$form.dispose()
