
#example execution:
#	powershell .\upload_dtsx.ps1 OvernightCleanup.dtsx server\mssql2008

$dtsxfile = $args[0]
$sqlserver = $args[1]

if ($sqlserver.Contains("\")) { $instanceName = "\"+$sqlserver.Split("\")[1] }

$xml = New-Object xml
$xml.Load(".\" + $dtsxfile)
$ns = New-Object Xml.XmlNamespaceManager $xml.NameTable
$ns.AddNamespace( "DTS", "www.microsoft.com/SqlServer/Dts" )

$plan_name = $xml.SelectSingleNode("DTS:Executable/DTS:Property[@DTS:Name = 'ObjectName']/text()", $ns).Value
$plan_id = $xml.SelectSingleNode("DTS:Executable/DTS:Property[@DTS:Name = 'DTSID']/text()", $ns).Value -replace "[{}]", ""
$subplan_id = $xml.SelectSingleNode("//DTS:Executable/DTS:Property[text() = 'Subplan_1']/../DTS:Property[@DTS:Name = 'DTSID']/text()", $ns).Value -replace "[{}]", ""

# nugget: have to escape *semicolon* as well as quotes in command line args... semicolon is the statement separator
# nugget: use the powershell community extensions "echoarg" commandlet to see how powershell is parsing out the command line

dtutil /quiet /file $dtsxfile /copy sql`;`"Maintenance Plans\$plan_name`" /destserver $sqlserver /destuser $env:SQLCMDUSER /destpassword $env:SQLCMDPASSWORD
# create the scheduled job for the plan
sqlcmd -S $sqlserver -v InstanceName=$instanceName -v PlanName=$plan_name -v PlanId=$plan_id -v SubPlanId=$subplan_id -i create_maint_plan_job.sql
		#debug: -Q "print 'InstanceName = `$(InstanceName), PlanName = `$(PlanName), PlanId = `$(PlanId), SubPlanId = `$(SubPlanId)'"
