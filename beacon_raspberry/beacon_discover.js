var Bleacon = require("bleacon");
Bleacon.startScanning('b9407f30f5f8466eaff925556b57fe6e');
//Bleacon.startScanning();
Bleacon.on("discover", function(bleacon) {
  //console.dir(bleacon);
  //console.log(bleacon.uuid);
  //console.log(bleacon.minor);
  //console.log(bleacon.accuracy);
  if(bleacon.major==8446 || bleacon.major==58842 || bleacon.major==2427 || bleacon.major==61343){
    //console.log(bleacon.major, bleacon.accuracy);
    var str = "http://lost-child.eu-gb.mybluemix.net/watchdog?client_id=";
    str += bleacon.uuid;
    str += "&major="
    str += bleacon.major;
    str += "&minor="
    str += bleacon.minor;
    str += "&location_id=2&distance="
    str += bleacon.accuracy
    let http = require('http');
    //URL = 'http://lost-child.eu-gb.mybluemix.net/watchdog?client_id=1234&major=123&minor=12&location_id=2&distance=0.23';
    http.get(str, (res) => {
      let body = '';
      res.setEncoding('utf8');
      res.on('data', (chunk) => {
        body += chunk;
      });
      res.on('end', (res) => {
        res = JSON.parse(body);
        //console.log(res);
      });
    }).on('error', (e) => {
      console.log(e.message); //エラー時
    });
  }
});
