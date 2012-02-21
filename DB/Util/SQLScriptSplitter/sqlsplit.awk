# example: gawk -f sqlsplit.awk file-to-split.sql
# set sqlsplit_use_db=databasename - to insert USE statement

BEGIN {
  outfile = "erase_me.sql" #start off with a dummy file to get the ball rolling
  IGNORECASE = 1
  functn = 0
}

END {
  #close off last file
  print "grant "grant" on "arr[1]" to public\ngo\n" >>outfile
  close(outfile)
  functn = 0
}

/\/***** Object:/ {
  #upon matcing the "object" comment, close off the previous output file
  print "grant "grant" on "arr[1]" to public\ngo\n" >>outfile
  close(outfile)

  #start up the new one
  match($0, /\[(.*)\]/, arr)
  outfile = arr[1]".sql"
  print "--$Author:$\n--$Date:$\n--$Modtime:$\n--$History:$\n" > outfile
  if (ENVIRON["sqlsplit_use_db"] != "") print "USE "ENVIRON["sqlsplit_use_db"]"\nGO\n" >>outfile
}

/^(create) +(proc|function|view)/ {

  grant = "execute"
  if ($2 == "view") grant = "select"

  printf "if not exists(select 1 from sysobjects where name = '"arr[1]"')\n  exec('create "$2" "arr[1] >>outfile

  # function is a little trickier because it could be a table or scalar return type requiring slightly different create function signature
  if ($2 == "function") {

    functn = 1  #flag that this is a function not a proc so that we can conditional stuff later (e.g. "set transation isolation level")
 
    lines = ""
    while((getline line) >0) {
      lines = lines"\n"line
      match(line, /returns/, a)
      if (a[0] != "returns") { continue }

      #debug: printf "line = %s, a[0] = %s, a[1] = %s, a[2] = %s, a[3] = %s\n", line, a[0], a[1], a[2], a[3]

      match(line, /table/, a)
      if (a[0] == "table") {
        grant = "select"
        print "() returns table as return select 1 as one')" >>outfile }
      else print "() returns int begin return 0 end')" >>outfile
      break

    }
  }

  #proc/view
  else {
    print " as select 1 as one')" >>outfile
  }

  print "GO" >>outfile

  sub(/create/, "alter") #change the create to alter
  sub(/$/, lines) #tack back on the lines "eaten" to figure out whether function was tabular or scalar
}

/AS BEGIN/ { #there has got to be a more elegant way to simply insert something after a match!?!
  if (functn == 0) {
    print >>outfile
    sub(/AS BEGIN/, "\nSET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED")
  }
}


{ 
  print  >>outfile
}

