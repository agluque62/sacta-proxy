/** */
angular.module("sacta_proxy")
    .controller("SprxStatusCtrl", function ($scope, $interval, $serv, $lserv) {
        /** Inicializacion */
        var ctrl = this;

        ctrl.history = null;
        ctrl.mainmsg = null;
        ctrl.webmsg = null;
        ctrl.depmsg = null;
        ctrl.pagina = "0";
        ctrl.SelectPage = (page) => {
            if (ctrl.history) {
                ctrl.history.destroy();
                ctrl.history = null;
            }
            if (page == 1) {
                ctrl.history = $('#history').DataTable({
                    ajax: HistoryData,
                    autoWidth: false,
                    searchBuilder: true,
                    columns: [
                        { "data": "Date", "width": "15%", "render": (data) => ctrl.datestr(data).str },
                        { "data": "Code", "width": "15%", "render": (data) => Enumerable.from(HistoryCodes).where(e => e.code == data).first().descr },
                        { "data": "User", "width": "10%"},
                        { "data": "Dep", "width": "10%" },
                        { "data": "State", "width": "10%" },
                        { "data": "Map", "width": "20%", "render": (map) => ctrl.normalizeMap(map).str },
                        { "data": "Cause", "width": "20%" }
                    ],
                    order: [[0, "desc"]],
                    dom: "<'row'<'col-md-12'Q>>" +
                        "<'row'<'col-md-6'l><'col-md-6'f>>" +
                        "<'row'<'col-md-12 small text-info'tr>>" +
                        "<'row'<'col-md-5'i><'col-md-7'p>>" +
                        "<'row historic-info'<'col-md-2'B>>",
                    buttons: [
                        {
                            extend: 'csv',
                            text: 'Exportar a csv',
                            className: 'btn btn-info'
                        }
                    ],
                    language: {
                        "decimal": "",
                        "emptyTable": "No hay datos disponibles",
                        "info": "Registros _START_ - _END_ de _TOTAL_",
                        "infoEmpty": "0 Registros",
                        "infoFiltered": "(_MAX_ Registros filtrados)",
                        "infoPostFix": "",
                        "thousands": ".",
                        "lengthMenu": "Mostrar _MENU_ Reg.",
                        "loadingRecords": "Loading...",
                        "processing": "Processing...",
                        "search": "Buscar:",
                        "zeroRecords": "No se han encontrado registros",
                        "paginate": {
                            first: "Primera",
                            last: "Ultima",
                            next: "Siguiente",
                            previous: "Anterior"
                        },
                        aria: {
                            sortAscending: ": Activar ordenado por conlumna ascendente",
                            sortDescending: ": Activar ordenado por columna descendente"
                        },
                        searchBuilder: {
                            add: 'Add Condicion',
                            condition: 'Condicion',
                            clearAll: 'Limpiar',
                            deleteTitle: 'Borrar',
                            data: 'Columna',
                            leftTitle: 'Left',
                            logicAnd: '&',
                            logicOr: '|',
                            rightTitle: 'Right',
                            title: {
                                0: 'Filtro',
                                _: 'Filtro (%d)'
                            },
                            value: 'Opcion',
                            valueJoiner: 'y'
                        }
                    }
                });
                //ctrl.history.searchBuilder.container().prependTo(ctrl.history.table().container());
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
            var str = mdate.format('YYYY-MM-DD, HH:mm:ss');
            return {str};
        };
        ctrl.shortdatestr = (date) => {
            var mdate = moment(date);
            var str = mdate.format('DD-MM, HH:mm:ss');
            return { str };
        };
        ctrl.sect = (sect)=>{
            var str = "";
            if (sect) {
                sect.forEach(element => {
                    str += (element.Sector + ":" + element.Position + ", ");
                });
            }
            return {str};
        };
        ctrl.seldepindex = undefined;
        ctrl.seldep = () => { return ctrl.deps()[ctrl.seldepindex]; };
        ctrl.deps = () => {
            var status = $lserv.GlobalStd().Status;
            return status ? status.ems : [];
        };

        ctrl.logs = function () {
            var win = window.open('logs/logfile.csv', '_blank');
            win.focus();
        };

        ctrl.normalizeMap = (map) => {
            var res = map.replace(/\s+/g, '');
            return { str: res.replace(/,/g, ', ') };
        };

        ctrl.lanbutton_text = (cfg, lan) => {
            if (cfg) {
                var lan_data = lan == 'Lan1' ? cfg.status.act.lan1 : cfg.status.act.lan2;
                return { str: lan + ' (' + lan_data.listen + ')' };
            }
            return lan;
        }

        ctrl.rx_text = (cfg, lan) => {
            var lan_data = lan == 'Lan1' ? cfg.status.act.lan1 : cfg.status.act.lan2;
            return { str: 'Recibe (' + lan_data.listen + ')' };
        }

        ctrl.tx_text = (cfg, lan) => {
            var lan_data = lan == 'Lan1' ? cfg.status.act.lan1 : cfg.status.act.lan2;
            return { str: 'Transmite (' + lan_data.sendto + ')' };
        }
        /** */
        function msgTableCreate(element) {
            var table = element.DataTable(
                {
                    data: [],
                    columns: [
                        { "data": "When", "width": "30%", "render": (data) => ctrl.shortdatestr(data).str },
                        { "data": "Msg", "width": "70%" }
                    ],
                    ordering: false,
                    language: { "emptyTable": "No hay Mensajes" },
                    dom: "t"
                });
            return table;
        }
        function msgTableRefresh(table, data) {
            table.clear().rows.add(data).draw()
        }
    /***/
        $scope.$on('$viewContentLoaded', function () {
            $serv.status((status) => {
                $lserv.GlobalStd(status);
                if (ctrl.seldepindex == undefined) ctrl.seldepindex = "0";
            });
        });

    /** Funcion Periodica del controlador */
        var timer = $interval(function () {

            if (ctrl.std().Status) {
                msgTableRefresh(ctrl.mainmsg, ctrl.std().Status.service.lst);
                msgTableRefresh(ctrl.webmsg, ctrl.std().Status.web.lst);
                msgTableRefresh(ctrl.depmsg, ctrl.seldep().status.global_state.lst);
            }
        }, pollingTime);

        // Cuando se carga la página....
        $scope.$on('$viewContentLoaded', () => {
            ctrl.mainmsg = msgTableCreate($('#mainmsg'));
            ctrl.webmsg = msgTableCreate($('#webmsg'));
            ctrl.depmsg = msgTableCreate($('#depmsg'));
        });

        /** Salida del Controlador. Borrado de Variables */
        $scope.$on("$destroy", function () {
            $interval.cancel(timer);
            ctrl.mainmsg.destroy();
            ctrl.webmsg.destroy();
            ctrl.depmsg.destroy();
        });

    });


