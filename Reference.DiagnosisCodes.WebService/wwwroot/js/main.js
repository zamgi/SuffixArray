$(document).ready(function () {
    var getMaxCount = function () {
        var maxCount = parseInt( trim_text( $('#maxCount').val() ) );
        if ( isNaN( maxCount ) ) {
            maxCount = 25; $('#maxCount').val( maxCount );
        }
        return (maxCount);
    };
    var getSuffix = function () { return trim_text($('#suffix').val()); };
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
        if (event.keyCode === $.ui.keyCode.TAB && $(this).data("autocomplete").menu.active) {
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

    if (!isGooglebot()) processing( getSuffix() ); 

    var last_processing_suffix = null;
    var last_processing_maxCount = null;
    function processing( suffix ) {
        var maxCount = getMaxCount();
        if (last_processing_suffix === suffix && last_processing_maxCount === maxCount)
            return;
        var timer = setTimeout( processing_start, 250 );
        //processing_start();
        var startDatetime = new Date();
        var model = {
            suffix  : suffix,
            maxCount: maxCount
        };
        $.ajax({
            type       : "POST",
            contentType: "application/json",
            dataType   : "json",
            url        : "/Rest/Run",
            data       : JSON.stringify( model ),
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
                    tableView( $processResult, tuples, elapsedMilliseconds );
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

    function tableView( $processResult, tuples, elapsedMilliseconds ) {
        $processResult.removeClass('error').text('');
        processing_end();
        if (tuples.values && tuples.values.length) {
            var html = 'delivered: <span class="border value">' + tuples.values.length + '</span>';
            if (tuples.findTotalCount !== tuples.values.length)
                html += ', found: <span class="border value">' + tuples.findTotalCount + '</span>';
            html += ', suffix: <span class="border value">' + tuples.suffix + '</span>';
            html += ', (search elapsed: <span class="border value">' + elapsedMilliseconds + '</span> ms)';
            $("#processHeader").html(html);

            var $table = $('<table />');
            $('<tr> <th>#</th> <th>Id</th> <th>Diagnosis</th> </tr>').appendTo($table);
            var trs = [], $d = $('<div>');
            for (var i = 0, len = tuples.values.length; i < len; i++) {
                var x = tuples.values[i];
                var tr = $('<tr class="suggest"><td>' + (i + 1) + '. </td><td class="id">' + x.id + '</td><td>' +
                    (x.suffixIdx ? ('<span class="suggest-query">' + $d.text( x.name.substr(0, x.suffixIdx) ).html() + '</span>') : '') +
                        '<span class="suggest-bold">' + $d.text( x.name.substr(x.suffixIdx, tuples.suffix.length) ).html() + '</span>' +
                        '<span class="suggest-query">' + $d.text( x.name.substr(x.suffixIdx + tuples.suffix.length) ).html() + '</span></td>' +
                        '</tr>');
                trs.push( tr );
            }
            $table.append( trs );
            $table.appendTo( $processResult );

            if (tuples.findTotalCount !== tuples.values.length)
                html = '<i>...more ' + (tuples.findTotalCount - tuples.values.length) + '...</i>';
            else
                html = '';
            $("#processFooter").html(html);
        } else {
            html = '<div class="no-suggest">diagnoses with the suffix <span class="suggest-bold">\'' + $d.text(tuples.suffix).html() + '\'</span> not found in Reference.DiagnosisCodes</div>';
            $processResult.html( html );
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
        $("#processHeader").html('<div class="processing-suggest"><img src="/images/roller.gif" /> Processing...</div>');
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
    function trim_text(text) { return text.replace(/(^\s+)|(\s+$)/g, ""); };
    function is_text_empty(text) { return !trim_text(text); };
    function isGooglebot() { return (navigator.userAgent.toLowerCase().indexOf('googlebot/') !== -1); };
});