for x in $*; do  
head -$COPYRIGHTLEN $x | diff copyright.txt - || ( ( cat LICENSE; echo; cat $x) > /tmp/file;  
mv /tmp/file $x )  
done