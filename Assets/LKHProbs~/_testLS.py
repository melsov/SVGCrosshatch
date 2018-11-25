import glob
import sys
import subprocess
import os

# if(len(sys.argv) < 2):
# 	print "tell me a name or pattern for files to delete please"
# 	exit(0)


# prefix = sys.argv[1]


# files = glob.glob(".\\%s" %(prefix));

# if len(files) == 0:
# 	print "no files matched %s" %(prefix)
# 	print "try *, wildcards etc?"
# 	exit(0)

# for f in files:
# 	print f

print "DID LIST THE ABOVE FILES"

os.system("touch _made-this.txt")

# response = raw_input("Y if you're sure you want to do this ('capital Y')")

# if response != "Y":
# 	print "goodbye"
# 	exit(0)



# for f in files:
# 	print f
# 	command = 'ls'
# 	output = ""
# 	try:
# 		full_command = "%s %s" %(command, f)
# 		print full_command
# 		os.system(full_command)
# 		# output = subprocess.check_output([command, f])
# 	except:
# 		print "exception for %s: %s" %(f, output)
# 		# try:
# 		# 	command = '.\\lkh.exe'
# 		# 	output = subprocess.check_output([command, f])
# 		# except:
# 		# 	print "no command worked. this script needs to know where the lkh executable is. (edit it or move the executable?) \\n also uses windows style slashes at the moment."
# 		# 	exit(1)
# 	print output
# exit(0)

