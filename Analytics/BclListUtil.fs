module BclListUtil 
  let ofArray (arr: 'T array) = new System.Collections.Generic.List<'T>(arr)
  let ofSeq (arr: 'T seq) = new System.Collections.Generic.List<'T>(arr)