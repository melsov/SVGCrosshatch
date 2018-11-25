import glob
import sys
import subprocess

if(len(sys.argv) < 2):
	print "tell me the prefix for the par and tsp files please"
	exit(0)


prefix = sys.argv[1]

if prefix.endswith(".par") == False:
	prefix = "%s*.par" %(prefixf)

files = glob.glob(prefix)


print files

for f in files:
	print f
	command = '.\\LKHBinary~\\lkh.exe'
	output = ""
	try:
		output = subprocess.check_output([command, f])
	except:
		try:
			command = '.\\lkh.exe'
			output = subprocess.check_output([command, f])
		except:
			print "no command worked. this script needs to know where the lkh executable is. (edit it or move the executable?) \\n also uses windows style slashes at the moment."
			exit(1)
	print output
exit(0)

