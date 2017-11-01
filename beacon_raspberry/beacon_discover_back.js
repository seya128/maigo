Bleacon = require("bleacon");
Bleacon.startScanning("B9407F30F5F8466EAFF925556B57FE6E")

console.log("start scanning")
console.log(Bleacon)

proximity_list = [ "immediate", "near", "far"]
Bleacon.on("discover", (bcon)->)
    if bcon.proximity is "immediate"
        console.log "ID: #{bcon.major}-#{bcon.minor}, 距離:#{bcon.proximity}"
        console.log "\t 信号強度: #{bcon.measuredPower}, #{bcon.rssi}, 精度:#{Math.ceil(bcon.accuracy)}"
