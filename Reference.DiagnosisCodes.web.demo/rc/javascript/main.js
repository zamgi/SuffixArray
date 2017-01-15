
$(document).ready(function () {
    var getMaxCount = function () {
        var maxCount = parseInt( trim_text( $('#maxCount').val().toString() ) );
        if ( isNaN( maxCount ) ) {
            maxCount = 25; $('#maxCount').val( maxCount );
        }
        return (maxCount);
    };
    var getSuffix = function () {
        var suffix = trim_text( $('#suffix').val().toString() );
        return (suffix);
    };
    var suffixOnChange = function () {
        var len = $("#suffix").val().length.toString().replace(/\B(?=(\d{3})+(?!\d))/g, " ");
        $("#inputTextLength").text("length: " + len + " chars ");
    };
    $("#suffix").focus(suffixOnChange).change(suffixOnChange).keydown(suffixOnChange).keyup(suffixOnChange).select(suffixOnChange).focus();
    var maxCountOnChange = function () {
        var suffix = getSuffix();
        if (MIN_SUFFIX_LENGTH <= suffix.length) {
            processing(suffix);
        }
    };
    $('#maxCount').change(maxCountOnChange).keyup(maxCountOnChange).select(maxCountOnChange);

    var MIN_SUFFIX_LENGTH = 2;
    $("#suffix")
    // don't navigate away from the field on tab when selecting an item
    .bind("keydown", function (event) {
        if (event.keyCode === $.ui.keyCode.TAB &&
                $(this).data("autocomplete").menu.active) {
            event.preventDefault();
        }
    })
    .autocomplete({
        delay: 500,
        search: function () {
            // custom minLength
            var suffix = getSuffix();
            if (MIN_SUFFIX_LENGTH <= suffix.length) {
                processing(suffix);
            }
            $(this).autocomplete("close");
            return (false);
        }
    });

    $('#tableView').click(function () {
        last_processing_suffix = null;
        last_processing_maxCount = null;
        processing( getSuffix() );
    });
    $('#mainPageContent').on('click', '#processButton', function () {
        if ($(this).hasClass('disabled')) return (false);
        last_processing_suffix = null;

        var suffix = getSuffix();
        if (is_text_empty(suffix)) {
            alert("    Enter the text    ");
            $("#suffix").focus();
            return (false);
        }
        
        processing( suffix );
    });

    if (!isGooglebot()) {
        //force_load_model();
        processing( getSuffix() );
    }

    var last_processing_suffix = null;
    var last_processing_maxCount = null;
    function processing( suffix ) {
        var maxCount = getMaxCount();
        if (last_processing_suffix == suffix && last_processing_maxCount == maxCount)
            return;
        var timer = setTimeout( processing_start, 250 );
        //processing_start();
        var startDatetime = new Date();

        var _tableView = $('#tableView').is(':checked');
        $.ajax({
            type: "POST",
            url:  "SuffixArrayHandler.ashx",
            data: {
                suffix  : suffix,
                maxCount: maxCount
            },
            success: function (tuples) {
                var endDatetime = new Date();
                var elapsedMilliseconds = endDatetime - startDatetime;

                clearTimeout(timer);
                var $processResult = $('#processResult');                
                $processResult.empty();
                if (tuples.err) {                    
                    processing_end();
                    $processResult.addClass('error').html('<div class="error-suggest"> ' + tuples.err + '</div>');
                } else {
                    last_processing_suffix = tuples.suffix;
                    last_processing_maxCount = tuples.maxCount;
                    if ( _tableView )                    
                        tableView( $processResult, tuples, elapsedMilliseconds );
                    else
                        divView( $processResult, tuples, elapsedMilliseconds );
                }
            },
            error: function () {
                clearTimeout(timer);
                $('#processResult').empty();                
                processing_end();
                $('#processResult').addClass('error').html('<div class="error-suggest">server error</div>');
            }
        });
    };

    function divView( $processResult, tuples, elapsedMilliseconds ) {
        $processResult.removeClass('error').text('');
        var _html = '';
        if (tuples.values && tuples.values.length != 0) {
            /*
            tuples.values.sort(function (a, b) {
                return (a.name < b.name) ? -1 : (a.name > b.name) ? 1 : 0;
            });
            */
            
            _html = 'delivered: <span class="border value">' + tuples.values.length + '</span>';
            if (tuples.findTotalCount != tuples.values.length) {
                _html += ', found: <span class="border value">' + tuples.findTotalCount + '</span>';
            }
            _html += ', suffix: <span class="border value">' + tuples.suffix + '</span>';
            _html += ', (search elapsed: <span class="border value">' + elapsedMilliseconds + '</span> ms)';
            $("#processHeader").html(_html);

            var array = [];
            for (var i = 0, len = tuples.values.length; i < len; i++) {
                var value = tuples.values[i];
                _html = '<div class="suggest"><span>' + (i + 1) + ']. </span>' +
                        '<span class="id">' + value.id + '</span>' +
                        ((value.suffixIdx != 0) ? ('<span class="suggest-query">' + value.name.substr(0, value.suffixIdx) + '</span>') : '') +
                        '<span class="suggest-bold">' + value.name.substr(value.suffixIdx, tuples.suffix.length) + '</span>' +
                        '<span class="suggest-query">' + value.name.substr(value.suffixIdx + tuples.suffix.length) + '</span>' +
                        /*
                        ', <span>' + value.type.toLowerCase() + '</span>' +
                        ', <span class="suggest-city-name">' + value.city.name + '</span>' +
                        ' <span>(' + value.city.type.toLowerCase() + ')</span>' +
                        */
                        '</div>';
                array.push(_html);
            }

            if (tuples.findTotalCount != tuples.values.length)
                _html = '<i>...ещё ' + (tuples.findTotalCount - tuples.values.length) + '...</i>';
            else
                _html = '';
            $("#processFooter").html(_html);
            _html = array.join('');
        } else {
            _html = '<div class="no-suggest">diagnoses with the suffix <span class="suggest-bold">\'' + tuples.suffix + '\'</span> not found in Reference.DiagnosisCodes</div>';
        }
        processing_end();
        $processResult.html(_html);
    };
    function tableView( $processResult, tuples, elapsedMilliseconds ) {
        $processResult.removeClass('error').text('');
        processing_end();
        if (tuples.values && tuples.values.length != 0) {
            var _html = 'delivered: <span class="border value">' + tuples.values.length + '</span>';
            if (tuples.findTotalCount != tuples.values.length)
                _html += ', found: <span class="border value">' + tuples.findTotalCount + '</span>';
            _html += ', suffix: <span class="border value">' + tuples.suffix + '</span>';
            _html += ', (search elapsed: <span class="border value">' + elapsedMilliseconds + '</span> ms)';
            $("#processHeader").html(_html);

            var $table = $('<table />');
            $('<tr> <th>#</th> <th>Id</th> <th>Diagnosis</th> </tr>').appendTo($table);
            for (var i = 0, len = tuples.values.length; i < len; i++) {
                var value = tuples.values[i];
                _html = '<tr class="suggest"><td>' + (i + 1) + ']. </td><td class="id">' +
                        value.id + '</td><td>' +
                        ((value.suffixIdx != 0) ? ('<span class="suggest-query">' + value.name.substr(0, value.suffixIdx) + '</span>') : '') +
                        '<span class="suggest-bold">' + value.name.substr(value.suffixIdx, tuples.suffix.length) + '</span>' +
                        '<span class="suggest-query">' + value.name.substr(value.suffixIdx + tuples.suffix.length) + '</span></td>' +
                        /*
                        '<td>' + value.type.toLowerCase() + '</td>' +
                        '<td class="suggest-city-name">' + value.city.name + '</td>' +
                        '<td>(' + value.city.type.toLowerCase() + ')</td>' +
                        */
                        '</tr>';
                $( _html ).appendTo( $table );
            }
            $table.appendTo( $processResult );

            if (tuples.findTotalCount != tuples.values.length)
                _html = '<i>...ещё ' + (tuples.findTotalCount - tuples.values.length) + '...</i>';
            else
                _html = '';
            $("#processFooter").html(_html);
        } else {
            _html = '<div class="no-suggest">diagnoses with the suffix <span class="suggest-bold">\'' + tuples.suffix + '\'</span> not found in Reference.DiagnosisCodes</div>';
            $processResult.html( _html );
        }        
    };

    var _$focused;
    function processing_start() {
        if (!_$focused) {
            _$focused = $(':focus');
            if (!_$focused.length && document.activeElement) _$focused = $(document.activeElement);
        }
        $('#suffix,#maxCount').addClass('no-change').attr('readonly', 'readonly').attr('disabled', 'disabled');
        $('#processResult').removeClass('error').html('');
        $('#processButton').addClass('disabled');
        $("#processHeader").html('<div class="processing-suggest"><img src="/roller.gif" /> Processing...</div>');
        $("#processFooter").html('');
    };
    function processing_end() {
        $('#suffix,#maxCount').removeClass('no-change').removeAttr('readonly').removeAttr('disabled');
        $('#processResult').removeClass('error').text('');
        $('#processButton').removeClass('disabled');
        $("#processHeader,#processFooter").html('');
        if (_$focused) {
            _$focused.focus();
            if (_$focused.length) document.activeElement = _$focused.get(0);
            _$focused = null;
        }
    };
    function trim_text(text) {
        return (text.replace(/(^\s+)|(\s+$)/g, ""));
    };
    function is_text_empty(text) {
        return (text.replace(/(^\s+)|(\s+$)/g, "") == "");
    };
    function split(val) {        
        return val.split(/ \s*/); //---return val.split(/,\s*/);
    };
    function extractLast(term) {
        return split(term).pop();
    };
    function force_load_model() {
        $.ajax({
            type: "POST",
            url: "SuffixArrayHandler.ashx",
            data: { suffix: "_dummy_" }
        });
    };
    function isGooglebot() {
        return (navigator.userAgent.toLowerCase().indexOf('googlebot/') != -1);
    };

    String.prototype.insert = function (index, str) {
        if (0 < index)
            return (this.substring( 0, index ) + str + this.substring( index, this.length ));
        return (str + this);
    };
    String.prototype.replaceAll = function (token, newToken, ignoreCase) {
        var _token;
        var str = this + "";
        var i   = -1;
        if (typeof token === "string") {
            if (ignoreCase) {
                _token = token.toLowerCase();
                while (( i = str.toLowerCase().indexOf( token, i >= 0 ? i + newToken.length : 0 )) !== -1) {
                    str = str.substring(0, i) + newToken + str.substring(i + token.length);
                }
            } else {
                return this.split(token).join(newToken);
            }
        }
        return (str);
    };
});