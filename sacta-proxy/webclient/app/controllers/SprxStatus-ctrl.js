/** */
angular.module("sacta_proxy")
    .controller("SprxStatusCtrl", function ($scope, $interval, $serv, $lserv) {
        /** Inicializacion */
        var ctrl = this;

        ctrl.history = null;
        ctrl.pagina = "0";
        ctrl.SelectPage = (page) => {
            if (ctrl.history) {
                ctrl.history.destroy();
                ctrl.history = null;
            }
            if (page == 1) {
                ctrl.history = $('#history').DataTable({
                    "ajax": HistoryData,
                    "autoWidth": false,
                    "columns": [
                        { "data": "Date", "width": "15%", "render": (data) => ctrl.datestr(data).str },
                        { "data": "Code", "width": "15%", "render": (data) => Enumerable.from(HistoryCodes).where(e => e.code == data).first().descr },
                        { "data": "User", "width": "10%"},
                        { "data": "Dep", "width": "10%" },
                        { "data": "State", "width": "10%" },
                        { "data": "Map", "width": "20%", "render": (map) => ctrl.normalizeMap(map).str },
                        { "data": "Cause", "width": "20%" }
                    ],
                    "language": {
                        "decimal": "",
                        "emptyTable": "No hay datos disponibles",
                        "info": "Registros _START_ - _END_ de _TOTAL_",
                        "infoEmpty": "0 Registros",
                        "infoFiltered": "(_MAX_ Registros filtrados)",
                        "infoPostFix": "",
                        "thousands": ".",
                        "lengthMenu": "Muestra _MENU_ registros",
                        "loadingRecords": "Loading...",
                        "processing": "Processing...",
                        "search": "Buscar:",
                        "zeroRecords": "No se han encontrado registros",
                        "paginate": {
                            "first": "Primera",
                            "last": "Ultima",
                            "next": "Siguiente",
                            "previous": "Anterior"
                        },
                        "aria": {
                            "sortAscending": ": Activar ordenado por conlumna ascendente",
                            "sortDescending": ": Activar ordenado por columna descendente"
                        }
                    }
                });
            }
            ctrl.pagina = page;
        }

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
            var str = mdate.format('DD-MM-YY, HH:mm:ss');
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

        ctrl.normalizeMap = (map) => {
            var res = map.replace(/\s+/g, '');
            return { str: res.replace(/,/g, ', ') };
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


