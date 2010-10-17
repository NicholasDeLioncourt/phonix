grammar Phonix;

/* Code generation rules */

options
{
    language=CSharp2;
}

tokens
{
    PLUS = '+';
    MINUS = '-';
    NULL = '*';
    EQ = '=';
    LBRACE = '[';
    RBRACE = ']';
    LPAREN = '(';
    RPAREN = ')';
    IMPORT = 'import';
    FEATURE = 'feature';
    SYMBOL = 'symbol';
    RULE = 'rule';
    SLASH = '/';
    POUND = '#';
    ARROW = '=>';
    UNDERSCORE = '_';
    BOUNDARY = '$';
}

@header
{
    using Phonix;
    using System.Collections.Generic;
}

@parser::namespace { Phonix.Parse }
@lexer::namespace { Phonix.Parse }

@members
{
    private Phonology _phono;
    private string _currentFile;
    private bool _parseError = false;
}

@rulecatch
{
catch (RecognitionException re) 
{
    _parseError = true;
    Console.Error.WriteLine(String.Format(
        "Couldn't parse {0} at {1} line {2} char {3}", 
        GetTokenErrorDisplay(re.Token), 
        _currentFile, 
        re.Line,
        re.CharPositionInLine)
    );
    Recover(input,re);
}
catch (PhonixException px)
{
    IToken token = input.LT(-1);
    Console.Error.WriteLine(String.Format("{0} at {1} line {2}", px.Message, _currentFile, token.Line));
    _parseError = true;
}
catch (ArgumentNullException)
{
    if (_parseError)
    {
        // after a parsing error, all bets are off, so we swallow this exception
    }
    else
    {
        throw;
    }
}
catch (NullReferenceException)
{
    if (_parseError)
    {
        // after a parsing error, all bets are off, so we swallow this exception
    }
    else
    {
        throw;
    }
}
}

/* Lexer Rules */

fragment DIGIT: '0'..'9';
fragment ALPHA: 'a'..'z' | 'A'..'Z';
fragment SPACE: ' ' | '\t';
fragment EOL: '\r' | '\n';
fragment RESERVED_START: '+' | '-' | '*' | '=' | '[' | ']' | '(' | ')' | '/' | '#' | '_' | '$' | '"' | '\'';
fragment RESERVED_MID: '[' | ']' | '(' | ')' | '=' | '$';
fragment CHAR_START: ~( SPACE | EOL | RESERVED_START );
fragment CHAR_MID: ~( SPACE | EOL | RESERVED_MID );
fragment SQ: '\'';
fragment DQ: '"';
NUMBER: (DIGIT)+;
LABEL: (ALPHA | DIGIT)+;
STR: CHAR_START (CHAR_MID)*;
QSTR: ( SQ ( ~SQ )* SQ ) | ( DQ ( ~DQ )* DQ );
COMMENT: POUND ~EOL* EOL { Skip(); };
WS: (SPACE | EOL)+ { Skip(); };

/* String rules */

str returns [string val]:
        STR { $val = $STR.text; }
    |   QSTR { $val = $QSTR.text.Substring(1, $QSTR.text.Length - 2); }
    |   LABEL { $val = $LABEL.text; }
    |   NUMBER { $val = $NUMBER.text; }
    ;

label returns [string val]:
        LABEL { $val = $LABEL.text; }
    |   NUMBER { $val = $NUMBER.text; }
    ;

/* Parser Rules */

parseRoot[string currentFile, Phonology phono]
    @init { _phono = $phono; _currentFile = $currentFile; }:
    phonixDecl* EOF
    { if (_parseError) throw new ParseException(currentFile); }
    ;

phonixDecl:
        importDecl
    |   featureDecl { _phono.FeatureSet.Add($featureDecl.val); }
    |   symbolDecl /* Adding to the SymbolSet is handled by ruleDecl itself */
    |   ruleDecl /* Adding to the RuleSet is handled by ruleDecl itself */
    ;

/* File import */

importDecl:
    IMPORT str
    { Util.ParseFile(_phono, _currentFile, $str.val); }
    ;

/* Feature declarations */

featureDecl returns [Feature val]: 
    FEATURE str paramList?
    { $val = Util.MakeFeature($str.val, $paramList.val, _phono); }
    ;

feature returns [Feature val]:
    str
    { $val = _phono.FeatureSet.Get<Feature>($str.val); }
    ;

unaryFeature returns [UnaryFeature val]:
    str
    { $val = _phono.FeatureSet.Get<UnaryFeature>($str.val); }
    ;

binaryFeature returns [BinaryFeature val]:
    str
    { $val = _phono.FeatureSet.Get<BinaryFeature>($str.val); }
    ;

scalarFeature returns [ScalarFeature val]:
    str
    { $val = _phono.FeatureSet.Get<ScalarFeature>($str.val); }
    ;

nodeFeature returns [NodeFeature val]:
    str
    { $val = _phono.FeatureSet.Get<NodeFeature>($str.val); }
    ;

/* Symbol declarations and usage */

symbolDecl: 
    SYMBOL str paramList? matrix
    { Util.MakeAndAddSymbol($str.val, $matrix.val, $paramList.val, _phono.SymbolSet); }
    ;

symbolStr returns [List<Symbol> val]
    @init { $val = new List<Symbol>(); }:
    str
    { $val = _phono.SymbolSet.SplitSymbols($str.val); }
    ;

/* Rule declarations */

ruleDecl: 
    RULE str paramList? rule
    { Util.MakeAndAddRule($str.val, $rule.action, $rule.context, $rule.excluded, $paramList.val, _phono.RuleSet); }
    ;

rule returns [List<IRuleSegment> action, RuleContext context, RuleContext excluded]:
    ruleAction { $action = $ruleAction.val; }
    (
        SLASH ( ctx=ruleContext { $context = $ctx.val; } SLASH? )?
        ( SLASH excl=ruleContext { $excluded = $excl.val; } )?
    )?
    ;

ruleAction returns [List<IRuleSegment> val]
    @init { 
        var left = new List<IMatrixMatcher>(); 
        var right = new List<IMatrixCombiner>(); 
    }: 
    (matchTerm { left.AddRange($matchTerm.val); })+
    ARROW 
    (actionTerm { right.AddRange($actionTerm.val); })+
    { $val = Util.MakeRuleAction(left, right); }
    ;

matchTerm returns [IEnumerable<IMatrixMatcher> val]:
        matchableMatrix { $val = new IMatrixMatcher[] { new MatrixMatcher($matchableMatrix.val) }; }
    |   symbolStr { $val = $symbolStr.val.ConvertAll<IMatrixMatcher>(s => s); }
    |   NULL { $val = new IMatrixMatcher[] { null }; }
    ;

actionTerm returns [IEnumerable<IMatrixCombiner> val]: 
        combinableMatrix { $val = new IMatrixCombiner[] { new MatrixCombiner($combinableMatrix.val) }; }
    |   symbolStr { $val = $symbolStr.val.ConvertAll<IMatrixCombiner>(s => s); }
    |   NULL { $val = new IMatrixCombiner[] { null }; }
    ;

ruleContext returns [RuleContext val]
    @init { $val = new RuleContext(); }:
    (BOUNDARY { $val.Left.Add(Word.LeftBoundary); })? 
    (left=contextTerm { $val.Left.AddRange($left.val); })* 
    UNDERSCORE
    (right=contextTerm { $val.Right.AddRange($right.val); })* 
    (BOUNDARY { $val.Right.Add(Word.RightBoundary); })?
    ;

contextTerm returns [IEnumerable<IRuleSegment> val]
    @init { $val = new List<IRuleSegment>(); }:
        matchableMatrix { $val = new IRuleSegment[] { new ContextSegment(new MatrixMatcher($matchableMatrix.val)) }; }
    |   symbolStr { $val = $symbolStr.val.ConvertAll<IRuleSegment>(s => new ContextSegment(s)); }
    |   multiTerm { $val = new IRuleSegment[] { $multiTerm.val }; }
    ;

multiTerm returns [MultiSegment val]
    @init { List<IRuleSegment> inner = new List<IRuleSegment>(); }:
    LPAREN
    (
        matchableMatrix { inner.Add(new ContextSegment(new MatrixMatcher($matchableMatrix.val))); }
    |   symbolStr { inner.AddRange($symbolStr.val.ConvertAll<IRuleSegment>(s => new ContextSegment(s))); }
    )+ 
    RPAREN { $val = new MultiSegment(inner, 0, 1); }
    (
        NULL { $val = new MultiSegment(inner, 0, null); }
    |   PLUS { $val = new MultiSegment(inner, 1, null); }
    )?
    ;

/* Feature matrices */

matrix returns [FeatureMatrix val] 
    @init { var fvList = new List<FeatureValue>(); }:
    LBRACE (featureVal { fvList.Add($featureVal.val); })* RBRACE
    { $val = new FeatureMatrix(fvList); }
    ;

matchableMatrix returns [IEnumerable<IMatchable> val]
    @init{ var fvList = new List<IMatchable>(); }:
    LBRACE 
    ( matchableVal { fvList.Add($matchableVal.val); } )* 
    RBRACE
    { $val = fvList; }
    ;

combinableMatrix returns [IEnumerable<ICombinable> val]
    @init{ var fvList = new List<ICombinable>(); }:
    LBRACE 
    ( combinableVal { fvList.Add($combinableVal.val); } )* 
    RBRACE
    { $val = fvList; }
    ;

/* Feature values */

featureVal returns [FeatureValue val]: 
        scalarVal { $val = $scalarVal.val; }
    |   binaryVal { $val = $binaryVal.val; }
    |   unaryVal { $val = $unaryVal.val; }
    |   nullVal
        { 
            if (!($nullVal.val is FeatureValue))
            {
                throw new FeatureTypeException($nullVal.val.ToString(), "null leaf feature");
            }
            $val = $nullVal.val as FeatureValue;
        }
    ;

matchableVal returns [IMatchable val]:
        scalarVal { $val = $scalarVal.val; }
    |   binaryVal { $val = $binaryVal.val; }
    |   bareVal { $val = $bareVal.val; }
    |   nullVal { $val = $nullVal.val; }
    |   variableVal { $val = $variableVal.val; }
    ;

combinableVal returns [ICombinable val]:
        scalarVal { $val = $scalarVal.val; }
    |   binaryVal { $val = $binaryVal.val; }
    |   unaryVal { $val = $unaryVal.val; }
    |   nullVal { $val = $nullVal.val; }
    |   variableVal { $val = $variableVal.val; }
    ;

scalarVal returns [FeatureValue val]: 
    scalarFeature EQ NUMBER { $val = $scalarFeature.val.Value(Int32.Parse($NUMBER.text)); }
    ;

binaryVal returns [FeatureValue val]: 
        (PLUS binaryFeature) { $val = $binaryFeature.val.PlusValue; }
    |   (MINUS binaryFeature) { $val = $binaryFeature.val.MinusValue; }
    ;

unaryVal returns [FeatureValue val]: 
    unaryFeature { $val = $unaryFeature.val.Value; };

nullVal returns [IMatchCombine val]:
    NULL feature { $val = $feature.val.NullValue; };

variableVal returns [IMatchCombine val]: 
    '$' feature { $val = $feature.val.VariableValue; };

/* the bareVal can be a unary match or a node-exists match. it exists here to
 * help the parser handle the ambiguity that otherwise results. 
 */
bareVal returns [IMatchable val]:
    feature 
    {
        if ($feature.val is NodeFeature) $val = ($feature.val as NodeFeature).ExistsValue;
        else $val = ($feature.val as UnaryFeature).Value;
    }
    ;

/* Parameters */

paramList returns [ParamList val]
    @init { $val = new ParamList(); }: 
    LPAREN 
    (param { $val.Add($param.str, $param.val); })* 
    RPAREN
    ;

param returns [string str, object val]: 
    label (EQ paramVal)?
    { $str = $label.val; $val = $paramVal.val; }
    ;

paramVal returns [object val]: 
        str { $val = $str.val; }
    |   matrix  { $val = $matrix.val; }
    ;