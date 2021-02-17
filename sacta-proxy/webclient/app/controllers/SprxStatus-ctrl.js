/** */
angular.module("sacta_proxy")
    .controller("SprxStatusCtrl", function ($scope, $interval, $serv, $lserv) {
        /** Inicializacion */
        var ctrl = this;

        ctrl.pagina = "0";

        /** Estados.. */
        ctrl.std = function () {
            return $lserv.GlobalStd();
        }
        ctrl.stds = (std) => {
            return {
                txt: std == 0 ? "Parado" : std == 1 ? "Running" : std == 2 ? "Error" : std + "???",
                css: std == 0 ? "bg-danger text-danger" : std == 1 ? "bg-success text-succes" : std == 2 ? "bg-danger text-danger" : std + "???"
                }
        };
        ctrl.isactive=(std)=>{
            return {
                txt: std == true ? "Si" :  "No",
                css: std == true ? "bg-success text-succes" : "bg-danger text-danger"
                }
        };
        ctrl.datestr = (date) => {
            var mdate = moment(date);
            var str = mdate.format('DD-MM-YY HH:mm:ss');
            return {str};
        };
        ctrl.sect = (sect)=>{
            var str = "";
            sect.forEach(element => {
                str += (element.Sector + ":" + element.Position + ", ");
            });
            return {str};
        };
        ctrl.seldepindex = undefined;
        ctrl.seldep = () => { return ctrl.deps()[ctrl.seldepindex]; };
        ctrl.deps = () => {
            return $lserv.GlobalStd().Status.ems;
        };

        ctrl.logs = function () {
            var win = window.open('logs/logfile.csv', '_blank');
            win.focus();
        };

        $scope.$on('$viewContentLoaded', function () {
            $serv.status((status) => {
                $lserv.GlobalStd(status);
                if (ctrl.seldepindex == undefined) ctrl.seldepindex = "0";
            });
        });

    /** Funcion Periodica del controlador */
        var timer = $interval(function () {

        }, pollingTime);

        /** Salida del Controlador. Borrado de Variables */
        $scope.$on("$destroy", function () {
            $interval.cancel(timer);
        });

    });


