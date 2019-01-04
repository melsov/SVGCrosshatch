import glob
import sys
import subprocess
import os

if(len(sys.argv) < 2):
	print "please provide a pattern or file name."
	print "files are found using 'glob.glob( <pattern> )' which accepts wildcards."
	print "'*' matches any set of characters (including none). example pattern: SomeFileName*"
	print "example usage: "
	print ""
	print "python %s SomeFileName*" % (sys.argv[0])
	print ""
	exit(0)


prefix = sys.argv[1]


files = glob.glob(".\\%s" % (prefix));

if len(files) == 0:
	print "no files matched %s" % (prefix)
	print "try *, wildcards etc?"
	exit(0)

# for f in files:
# 	print f

print "WILL DELETE %d FILES" % (len(files))

# response = raw_input("Y if you're sure you want to do this ('capital Y')")

# if response != "Y":
# 	print "nothing deleted. bye."
# 	exit(0)



for f in files:
	command = 'del'

	try:
		full_command = "%s %s" % (command, f)
		print full_command
		os.system(full_command)

	except:
		print "exception for %s" % (f)



exit(0)

