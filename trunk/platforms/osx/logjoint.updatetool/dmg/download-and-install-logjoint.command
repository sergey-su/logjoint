flavour=''
color='\033[0;32m'
nocolor='\033[0m'
mono=$(which mono)
tmpZip=$TMPDIR/logjoint-update.zip
tmpUpdateInfo=$TMPDIR/update-info.xml
updateUrl=https://publogjoint.blob.core.windows.net/updates/logjoint-mac${flavour}.zip

printf "${color}downloading latest application binaries...${nocolor}\n"
cat > $tmpUpdateInfo <<EOF
<?xml version="1.0" encoding="utf-8"?>
<root binaries-etag="$(curl -D- -o $tmpZip $updateUrl | grep ETag | cut -f2 -d" " | tr -d "\r\n")"
last-check-timestamp="$(date -u "+%Y-%m-%dT%H:%M:%S.00000Z")"
/>
EOF

printf "${color}copying binaries to user Applications folder...${nocolor}\n"
mkdir -p ~/Applications/logjoint.app
unzip -d ~/Applications/logjoint.app/Contents -o $tmpZip
chmod +x ~/Applications/logjoint.app/Contents/MacOS/logjoint
mv $tmpUpdateInfo ~/Applications/logjoint.app/Contents/MonoBundle

if [[ ! -f $mono ]] || [[ -z $(mono -V | egrep "compiler version ((4\.[2-9])|[5-9])") ]]; then
  printf "${color}downloading mono framework...${nocolor}\n"
  curl "http://download.mono-project.com/archive/mdk-latest.pkg" -o $TMPDIR/mdk.pkg
  printf "${color}installing mono framework...${nocolor}\n"
  sudo installer -pkg $TMPDIR/mdk.pkg -target /
fi

printf "${color}jitting application binaries...${nocolor}\n"
~/Applications/logjoint.app/Contents/MacOS/logjoint --touch

open ~/Applications/logjoint.app

printf "\n${color}installation completed successfully!${nocolor}\n"